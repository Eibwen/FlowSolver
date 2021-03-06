<Query Kind="Program">
  <NuGetReference>FubuCore</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Drawing</Namespace>
</Query>

//TODO support bridges
//TODO Enhancements:
//  Edge following... postit note idea for that
//     Edge following pre-processor, IF it CAN follow an edge, only keep paths that cover all those edges
//     BUT do need to keep any that fit that, which could be multiple, idealy order by minimums too
//     CONCLUSION: probably not worth it, because all this does is sort the possible paths in a column... this would be wanted for a logic solver
//  Find single lines enclosed by a path (>7 cells taken out of 8)
//  Order by minimum lengths... is it that already? should be breath-first
//  If a endpoint is the only endpoint who can get to a square, MUST be one of those paths
//     Very common, any U shapes around a square next to corner or anything
//     Also "------y--y-g..."
//     i.e. one way to do it anyway...  For each dancing links cell column, if all the rows in that column are only one endpoint pair, any paths of that endpoint pair that are not in that column are impossible
//  Foreach cell that is not in singlular path and not an endpoint, must have 2 sides open?
//  DONE-Foreach cell that is not in a singular path, must have at least 1 side open
//  Foreach singular path, each endpoint other than current path, must have at least 1 side open (redundant with above?)
//    -i.e. If a path blocks off ALL of the 4 NSWE for another endpoint
//  PossiblePaths could be generated in parallel, on multiple threads
//  Other optimizations of PossiblePaths***
//    NEARLY_DONE-Used shared instances and '=='-Use ints instead of coords?  or a struct only having an int
//    Use a tree structure for each flow, instead of SO MANY Lists... maybe can filter that beforehand, otherwise just read from each leaf to the parent...
//      Order doesn't matter... so can build the master tree, and just return LeafEndpoint objects that are IEnumerable
//  Pre-filtering possible path generation could save huge amounts of time
//    How to do?  Maybe pattern matching, like NNWWSSEE makes a square around a single cell, so is impossible for any following paths
//    Can't easily do partial path filtering, since it only reduces the future movements, generally doesn't eliminate all sub-paths
//    Use a profiler to see how much memory/cpu the generation is using, and is pre-filtering worth it
//      If could reduce memory usage, and other optimizations***, might be able to generate all the paths no matter what quickly, then filter the IEnumerable

//BUGS:
//  FIXED--Flow color finder is not working, always gets new color
//  FIXED--Flow color finder probably does not use loaded results...
//  Not using OnlyOne filters
//  FALSE-did this on two computers--Possibly not using EnclosedCell filter?


//#define STACKON

void Main()
{
	//PossiblePathsTests.TupleTest();
	PossiblePathsTests.CoordsTest();
	//PossiblePathsTests.Test4();
	//PossiblePathsTests.TestRealBoard();
	//PossiblePathsTests.TestRealBoardLoading();
	//PossiblePathsTests.TestFindPathsFailure();
	
	//fffff
	RunSolution();
	
	
	//Each row will have which cells its using, and then which color it is
	//So dancing links columns are:
	//  cells (height * width)
	//  color count
}

List<string> SampleBoards = new List<string>
{
	/*0 easyish*/
	"---g-r-y--rb----g----by-------------",
	/*1 easy */
	"b---------------y---ygr--g-r-----b--",
	/*2 extremely easy*/
	"byg-c-----g-------o--cm--by-r--or-m-", //6x6 Mania, 108, not a good test board...
	/*3 easy*/
	"brbogy-------r----------go---------y", //6x6 Mania, 110
	/*4 lots of solutions*/
	"b-----r---g--------gbr--------------", //6x6 Mania, 111
	
	"o-----yco------y-b-g--g---r----------b----c-----r", //7x7 53
	"---------y-gr---b-----y---------r--gb------------", //7x7 96, multiple solutions?
	"--ogy-y-b--------b-rc------r--g--o------c--------", //7x7 101, maybe could make "Only I can Go here" filter algorithm
	"--------------b-g------y-g---br-y---------r------", //7x7 111, many options to consider
	"o--o-----c--b---r-b------yc--r-y----g---------g--", //7x7 115
	//10
	"---gy-----------y-b--g-r------------b---r--------", //7x7 117
	"rg----------g-----b-------------------y--y-r----b", //7x7 130
	"--g-----o--c----grb---or-----yc-b----y-----------", //7x7 
	"---------g--c--go----oc--y--r-r---------b-by----- ", //7x7 149
	
	//CANNOT DO 8x8 still... at least not within 1 minute
	"b--------g---------o-y-------b-----r-g----y-----------o--------r", //Extreme 23, haven't gotten solution
	"---------b---------g---------------r------b-----y------------gyr", //Extreme 22
	"----------ybg---o-------------------------------ry---r-o----g-b------------------", //Extreme 9x9 20
	
};

public BoardMask LoadBoard(int index)
{
	var flowBoard = SampleBoards[index];
	int sizeSquare = (int)Math.Sqrt(flowBoard.Length);
	return new BoardMask(sizeSquare, sizeSquare, flowBoard);
}

static bool USE_CAN_REACH_FILTERING = true;
static bool USE_ORPHAN_CELLS_FILTERING = true;
static bool USE_ONLY_ONE_PATH_REACHES_CELL = true; 
static bool USE_COLOR_ALWAYS_GOES_THROUGH_CELL = true; //Therefore it MUST use this cell, nobody else can
public void RunSolution()
{
	var board = LoadBoard(0);
	
	var pathsGenerators = board.Flows.Values.Select((x, n) => new PossiblePaths(n, x.Start, x.End, board));
	
	var solver = new FlowSolver(board, pathsGenerators);
	solver.DrawBoard();
	
	solver.BuildTableFull();
	
	var postBuildTableLinksFilter = new FlowFilter_PostDancingLinks_OnlyOne(solver);
	//postBuildTableLinksFilter.RunFilter().Dump("REMOVED COUNT");
	//"===============".Dump();
	//postBuildTableLinksFilter.RunFilter().Dump("REMOVED COUNT");
	int removedCount;
	do
	{
		//TODO Really only 2 times of this filter adds the most benefit it appears
		//   Although mostly because i'm dumping so much i'm sure...
		"===============".Dump();
		removedCount = postBuildTableLinksFilter.RunFilter().Dump("REMOVED COUNT");
	}
	while (removedCount > 0);
	
	//solver.ToStringOutput().Dump();
	solver.Search();
}


public class PossiblePathsTests
{
	public static void TupleTest()
	{
		var a = new Tuple<int, int>(23, 53);
		var b = new Tuple<int, int>(23, 53);
		
		Debug.Assert(a == b, "== operator");
		Debug.Assert(a.Equals(b), "Equals operator");
	}
	public static void CoordsTest()
	{
		var a = Coords.Create(23, 53);
		var b = Coords.Create(23, 53);
		var c = Coords.Create(23, 54);
		
		//Debug.Assert(a == b, "== operator");
		Debug.Assert(a.Equals(b), "Equals operator");
		Debug.Assert(!c.Equals(b), "Equals operator");
		
		Debug.Assert(a.GetHashCode().Equals(b.GetHashCode()), "GetHashCode operator");
		Debug.Assert(!c.GetHashCode().Equals(b.GetHashCode()), "GetHashCode operator");
	}
	
	public static void Test4()
	{
		//Expect:
		//  (0,0), (0,1)
		//  (0,0), (1,0), (1,1), (0,1)
		var boardStr = "x-x-";
		var board = new BoardMask(2, 2);
		
		var start = Coords.Create(0, 0);
		var end = Coords.Create(0, 1);
		
		board.MarkFilled(start);
		board.MarkFilled(end);
		
		var paths = new PossiblePaths(99, start, end, board);
		paths.FindPaths().Dump("Result");
	}
	
	public static void TestRealBoard()
	{
		//Arrange
		var flowBoard = "---g-r-y--rb----g----by-------------";
		var findPathsFor = 'g';
		var endpoints = new List<Coords>();
		
		var board = new BoardMask(6, 6);
		
		for (int x = 0; x < board.Width; ++x)
		for (int y = 0; y < board.Height; ++y)
		{
			var value = flowBoard[y*board.Width + x];
			if (value != '-')
			{
				board.MarkFilled(Coords.Create(x, y));
			}
			if (value == findPathsFor)
			{
				endpoints.Add(Coords.Create(x, y));
			}
		}
		
		endpoints.Dump("Endpoints");
		Debug.Assert(endpoints.Count == 2, "Should have 2 endpoints");
		
		
		var paths = new PossiblePaths(99, endpoints[0], endpoints[1], board);
		//paths.FindPaths().Dump("Result");
		paths.FindPaths().Count().Dump();
	}
	
	public static void TestRealBoardLoading()
	{
		//Arrange
		var flowBoard = "---g-r-y--rb----g----by-------------";
		var findPathsFor = 'g';
		var endpoints = new List<Coords>();
		
		var board = new BoardMask(6, 6, flowBoard);
		
		
		for (int i = 0; i < flowBoard.Length; ++i)
		{
			if (flowBoard[i] == findPathsFor)
			{
				endpoints.Add(board.CellIdToCoords(i));
			}
		}
		endpoints.Dump("Endpoints");
		Debug.Assert(endpoints.Count == 2, "Should have 2 endpoints");
		
		
		var paths = new PossiblePaths(99, endpoints[0], endpoints[1], board);
		//paths.FindPaths().Dump("Result");
		paths.FindPaths().Count().Dump();
	}
	
	public static void TestFindPathsFailure()
	{
		var flowBoard = "a-ab----b";
		var board = new BoardMask(3, 3, flowBoard);
		
		var pathsGenerators = board.Flows.Values.Select((x, n) => new PossiblePaths(n, x.Start, x.End, board));
		
		foreach (var pg in pathsGenerators)
		{
			pg.PathId.Dump("Flow");
			Util.HorizontalRun(true, pg.Start, pg.End).Dump();
			pg.FindPaths().Dump();
		}
		
//		var solver = new FlowSolver(board, pathsGenerators);
//		solver.BuildTableFull();
//		solver.ToStringOutput().Dump();
	}
}

public class PossiblePaths
{
	public PossiblePaths(int pathId, Coords start, Coords end, BoardMask board)
	{
		PathId = pathId;
		Start = start;
		End = end;
		Board = board;
	}
	
	public readonly int PathId;
	public readonly Coords Start;
	public readonly Coords End;
	public readonly BoardMask Board;
	
	
	Stack<Coords> queue = new Stack<Coords>();
	HashSet<Coords> visited = new HashSet<Coords>();
	
	public IEnumerable<IList<Coords>> FindPaths()
	{
		foreach (var path in FindPaths_RawResults())
		{
			if (CountAsValidPath_OtherCanBeReached(Start, End, path))
				yield return path;
		}
		//TODO any others get thrown away?... count those to see increases?
	}
	
	public IEnumerable<IList<Coords>> FindPaths_RawResults()
	{
		queue.Push(Start);
		visited.Add(Start);
		
		return FindPaths_Recurse(NextSteps(Start));
	}
	
	IEnumerable<IList<Coords>> FindPaths_Recurse(IEnumerable<Coords> nextSteps)
	{
		foreach (var step in nextSteps)
		{
			queue.Push(step);
			visited.Add(step);
			
			if (step == End)
			{
				//"Found possible path".Dump();
				yield return new List<Coords>(queue);
			}
			else
				foreach (var result in FindPaths_Recurse(NextSteps(step)))
					yield return result;
			
			//throw new Exception("Need dynamic programming here...");
			//TODO Need dynamic programming here...
			queue.Pop();
			visited.Remove(step);
		}
	}
	
	IEnumerable<Coords> NextSteps(Coords current)
	{
		foreach (var c in AllNextSteps(current))
		{
			//c.Dump("step");
			//End.Dump("end");
			//Util.HorizontalRun(true, c.Equals(End), !visited.Contains(c), !Board.IsFilled(c)).Dump("compare");
			
			if (c == End)
				yield return c;
			else if (!visited.Contains(c) && !Board.IsFilled(c))
			{
				yield return c;
			}
		}
	}
	//TODO build service that does this... doesn't seem quite a low-level as CellTranslator is
	IEnumerable<Coords> AllNextSteps(Coords current)
	{
		//current.Dump();
		//West
		if (current.X > 0)
			yield return Coords.Create(current.X - 1, current.Y);
		//East
		if (current.X < Board.Width-1)
			yield return Coords.Create(current.X + 1, current.Y);
		//North
		if (current.Y > 0)
			yield return Coords.Create(current.X, current.Y - 1);
		//South
		if (current.Y < Board.Height-1)
			yield return Coords.Create(current.X, current.Y + 1);
	}
	
	
	bool CountAsValidPath_OtherCanBeReached(Coords start, Coords end, IList<Coords> path)
	{
		var canReach = new FlowFilter_PassagesCanReach(Board, path);
		var orphanedCells = new FlowFilter_OrphanCells(Board, path);
		
		//TODO THIS SHOULD GO INSIDE OF canReach.IsFiltered()
		//TODO is there a better way to get the flows at this point?
		foreach (var flow in Board.Flows.Values)
		{
			if (flow.Start == start
				|| flow.End == end
				|| flow.Start == end
				|| flow.End == start)
			{
				//Cannot test THIS path right now
				//  Extra safe way to test this, by checking all permutation of endpoints
				continue;
			}
			
			
			if (USE_CAN_REACH_FILTERING && !canReach.CanBeReached(flow))
			{
				//"filtered path".Dump();
				//Cannot be reached, this path blocks other flows completely, return false
				return false;
			}
		}
		
		if (USE_ORPHAN_CELLS_FILTERING && orphanedCells.IsFiltered())
		{
			return false;
		}
		
		//Everyone passes the tests here
		return true;
	}

	
//	bool IsEdgeNode(Coords current)
//	{
//		//TODO also check if routes around it are blocked, and consider the edge node endpoints to be those
//		//  Note DO NOT count the same color then...
//		
//		//fuck, find all the endpoints
//		//  find where this line crosses it
//		//  and the other endpoint should cross each line/column on the same way
//		
//		return current.Y == 0
//			|| current.Y == Board.Height-1
//			|| current.X == 0
//			|| current.X == Board.Width-1;
//	}
}
public class BoardMask : CellTranslator
{
	public BoardMask(int width, int height)
		: base(width)
	{
		Width = width;
		Height = height;
		_array = new BitArray(Width * Height);
		
		FlowColorFinder = new FlowColorFinder();
	}
	
	public BoardMask(int width, int height, string boardDescription)
		: this(width, height)
	{
		LoadBoardDescription(boardDescription);
	}
	
	public readonly int Width;
	public readonly int Height;
	
	bool _boardLoaded = false;
	public int ColorCount
	{
		get
		{
			ThrowIfNotLoaded();
			return Flows.Count;
		}
	}
	
	public Dictionary<char, FlowEndpoint> Flows { get; private set; }
	BitArray _array;
	
	public FlowColorFinder FlowColorFinder { get; private set; }
	
	char[] EmptyCellChars = new [] { '-', ' ', '_' };
	public void LoadBoardDescription(string boardDescription)
	{
		var colorChars = new Dictionary<char, FlowEndpoint>();
		
		for (int x = 0; x < Width; ++x)
			for (int y = 0; y < Height; ++y)
			{
				var coords = Coords.Create(x, y);
				var value = boardDescription[y*Width + x];
				
				if (!EmptyCellChars.Contains(value))
				{
					MarkFilled(coords);
					
					if (!colorChars.ContainsKey(value))
						colorChars.Add(value, new FlowEndpoint(value, coords));
					else
					{
						colorChars[value].SetEnd(coords);
					}
				}
			}
		
		Flows = colorChars;
		
		FlowColorFinder.LoadAndSetFlows(Flows);
		
		_boardLoaded = true;
	}
	
	public bool IsFilled(Coords cell)
	{
		ThrowIfNotLoaded();
		return _array[CoordsToCellId(cell)];
	}
	public void MarkFilled(Coords cell)
	{
		_array[cell.X + Width*cell.Y] = true;
	}
	
	public void ThrowIfNotLoaded()
	{
		if (!_boardLoaded)
			throw new Exception("Failcakes");
	}
}
public class CellTranslator
{
	public CellTranslator(int boardWidth)
	{
		Width = boardWidth;
	}
	
	readonly int Width;
	
	public int CoordsToCellId(Coords cell)
	{
		return cell.X + Width*cell.Y;
	}
	public Coords CellIdToCoords(int cellId)
	{
		return Coords.Create(cellId % Width, cellId / Width);
	}
}
public class Coords
{
	private Coords(int x, int y)
	{
		X = x;
		Y = y;
	}
	public readonly int X;
	public readonly int Y;
	
	
	//Using the 2d array lookup seems to help about 0.2-0.4 on a 6x6 puzzle
	//Using == instead of Equals, on immutable objects seemed around 1 second?
	static OnDemand2dArray<Coords> CoordsLookup = new OnDemand2dArray<Coords>();
	public static Coords Create(int x, int y)
	{
		if (CoordsLookup.Contains(x, y))
			return CoordsLookup.Get(x, y);
		else
		{
			var coord = new Coords(x, y);
			CoordsLookup.Add(x, y, coord);
			return coord;
		}
	}
	
//	public override bool Equals(object obj)
//	{
//		var coord = obj as Coords;
//		if (coord == null) return false;
//		return this.X == coord.X && this.Y == coord.Y;
//	}
	public override int GetHashCode()
	{
		return GetHashCode(X, Y);
	}
	public static int GetHashCode(int x, int y)
	{
		return unchecked((x.GetHashCode() << 16) + y.GetHashCode());
	}
}
public class OnDemand2dArray<T>
{
	List<List<T>> array = new List<List<T>>();
	
	public bool Contains(int x, int y)
	{
		if (array.Count <= x) return false;
		if (array[x].Count <= y) return false;
		return array[x][y] != null;
	}
	
	public T Get(int x, int y)
	{
		return array[x][y];
	}
	
	public void Add(int x, int y, T obj)
	{
		while (array.Count <= x)
		{
			array.Add(new List<T>());
		}
		while (array[x].Count <= y)
		{
			array[x].Add(default(T));
		}
		array[x][y] = obj;
	}
}
public class FlowEndpoint
{
	public FlowEndpoint(char flow, Coords start)
	{
		Flow = flow;
		Start = start;
	}
	
	public readonly char Flow;
	public readonly Coords Start;
	public Coords End { get; private set; }
	
	public Color Color { get; set; }
	
	public void SetEnd(Coords coords)
	{
		End = coords;
	}
}

public class PathDescription
{
	public PathDescription(IList<Coords> path, int boardWidth, int boardHeight)
	{
		Path = path;
		PathCells = BuildPathCells(path, boardWidth, boardHeight);
		cellTranslator = new CellTranslator(boardWidth);
	}
	
	BitArray BuildPathCells(IList<Coords> path, int boardWidth, int boardHeight)
	{
		var pathCells = new BitArray(boardWidth * boardHeight);
		foreach (var cell in path)
		{
			pathCells[cell.X + boardWidth*cell.Y] = true;
		}
		return pathCells;
	}
	
	//FOR DEBUG USE ONLY...
	IEnumerable<Coords> Path { get; set; }
	
	readonly CellTranslator cellTranslator;
	public readonly BitArray PathCells;
	
	public bool IsInPath(Coords cell)
	{
		return PathCells[cellTranslator.CoordsToCellId(cell)];
	}
	
	public void DebugShowPath()
	{
		var renderer = new FlowRenderer();
		renderer.Render(6, 6, new [] { new FlowPath('a', Path) });
	}
}
public class FlowFilter_PassagesCanReach : FlowFilterBase
{
	public FlowFilter_PassagesCanReach(BoardMask board, IList<Coords> path)
		: base(board.Width, board.Height)
	{
		Board = board;
		
		pathDescription = new PathDescription(path, Board.Width, Board.Height);
	}
	
	BoardMask Board { get; set; }
	
	PathDescription pathDescription;
	Dictionary<Coords, HashSet<Coords>> DynamicProgrammingLookup = new Dictionary<Coords, HashSet<Coords>>();
	
	//TODO rename this to IsFiltered?
	public bool CanBeReached(FlowEndpoint flow)
	{
		return GetTouchingCells(flow.Start).Contains(flow.End);
	}
	
	
	HashSet<Coords> GetTouchingCells(Coords cell)
	{
		if (DynamicProgrammingLookup.ContainsKey(cell))
			return DynamicProgrammingLookup[cell];
		else
		{
			var cellGroup = FindTouchingCoords(cell);
			foreach (var neighbor in cellGroup)
			{
				try
				{
					DynamicProgrammingLookup.Add(neighbor, cellGroup);
					
					//var renderer = new FlowRenderer();
					//renderer.Render(6, 6, new [] { new FlowPath('a', cellGroup) });
				}
				catch
				{
					cell.Dump("Starting from cell");
					neighbor.Dump();
					cellGroup.Dump();
					//DynamicProgrammingLookup.Dump();
					var renderer = new FlowRenderer();
					DynamicProgrammingLookup.Values.Each(x => renderer.Render(6, 6, new [] { new FlowPath('a', x) }, Board.FlowColorFinder));
					pathDescription.DebugShowPath();
					throw;
				}
			}
			
			return cellGroup;
		}
	}
	
	
	
	
	public HashSet<Coords> FindTouchingCoords(Coords start)
	{
		var visited = new HashSet<Coords>();
		var toCheck = new Queue<Coords>();
		
		Action<Coords> nextStepsOperation = x =>
			{
				toCheck.Enqueue(x);
				visited.Add(x);
			};
		
		visited.Add(start);
		NextSteps(visited, start).Each(nextStepsOperation);
		while (toCheck.Count > 0)
		{
			var cell = toCheck.Dequeue();
			//visited.Add(cell);
			
			NextSteps(visited, cell).Each(nextStepsOperation);
		}
		
//		var renderer = new FlowRenderer();
//		renderer.Render(6, 6, new [] { new FlowPath('a', Path), new FlowPath('b', visited) });
//		renderer.Render(6, 6, new [] { new FlowPath('b', visited), new FlowPath('a', Path) });
//		Path.Dump();
		
		return visited;
	}
	
	//Nearly copied from PossiblePaths... could be merged into a single service class
//	HashSet<Coords> visited = new HashSet<Coords>();
//	public IEnumerable<Coords> FindTouchingCoords_RawResults(Coords start)
//	{
//		visited.Add(start);
//		
//		return FindTouchingCoords_Recurse(NextSteps(start));
//	}
//	
//	IEnumerable<Coords> FindTouchingCoords_Recurse(IEnumerable<Coords> nextSteps)
//	{
//		foreach (var step in nextSteps)
//		{
//			visited.Add(step);
//			
//			//Don't need to bother with the return value from sub functions, since sharing same HashSet
//			FindTouchingCoords_Recurse(NextSteps(step));
//		}
//		return visited;
//	}
	
	//TODO also in PossiblePaths... add a Func<Coords, bool> for filtering them?
	IEnumerable<Coords> NextSteps(HashSet<Coords> visited, Coords current)
	{
		Func<Coords, bool> shouldInclude = c => (!visited.Contains(c)
			//&& !Board.IsFilled(c)
			&& !pathDescription.IsInPath(c));
		return FilteredNextSteps(current, shouldInclude);
	}
}
public class FlowFilter_OrphanCells : FlowFilterBase
{
	public FlowFilter_OrphanCells(BoardMask board, IList<Coords> path)
		: base(board.Width, board.Height)
	{
		pathDescription = new PathDescription(path, board.Width, board.Height);
	}
	
	readonly PathDescription pathDescription;
	
	public bool IsFiltered()
	{
		//So only return if its not in path, and not going off the edge.
		//  Will be zero Coords returned if a cell is totally blocked off
		Func<Coords, bool> shouldInclude = c => !pathDescription.IsInPath(c);
		
		var cannotBeOrphaned = AllBoardCoords().Where(x => !pathDescription.IsInPath(x));
		//TODO TODO, if it is NOT an endpoint, it MUST have at least 2?
		return cannotBeOrphaned.Any(x => !FilteredNextSteps(x, shouldInclude).Any());
	}
	
	///All of the coordinates on the board, INCLUDING endpoint ones
	IEnumerable<Coords> AllBoardCoords()
	{
		for (int x = 0; x < Width; ++x)
			for (int y = 0; y < Height; ++y)
				yield return Coords.Create(x, y);
	}
}
public abstract class FlowFilterBase
{
	public FlowFilterBase(int boardWidth, int boardHeight)
	{
		Width = boardWidth;
		Height = boardHeight;
	}
	
	public readonly int Width;
	public readonly int Height;
	
	//TODO also in PossiblePaths... as just NextSteps
	protected IEnumerable<Coords> FilteredNextSteps(Coords current, Func<Coords, bool> shouldInclude)
	{
		foreach (var c in AllNextSteps(current))
		{
			if (shouldInclude(c))
			{
				yield return c;
			}
		}
	}
	
	//TODO this also exists in PossiblePaths
	protected IEnumerable<Coords> AllNextSteps(Coords current)
	{
		//current.Dump();
		//West
		if (current.X > 0)
			yield return Coords.Create(current.X - 1, current.Y);
		//East
		if (current.X < Width-1)
			yield return Coords.Create(current.X + 1, current.Y);
		//North
		if (current.Y > 0)
			yield return Coords.Create(current.X, current.Y - 1);
		//South
		if (current.Y < Height-1)
			yield return Coords.Create(current.X, current.Y + 1);
	}
}

public class FlowFilter_PostDancingLinks_OnlyOne : PostDancingLinksFilterBase
{
	public FlowFilter_PostDancingLinks_OnlyOne(FlowSolver solver)
		: base(solver)
	{
	}
	
	public override int RunFilter()
	{
		var removedRowCount = 0;
		foreach (var column in Solver.Columns)
		{
			if (column.Count == 1)
			{
				//If its already at one, no point in checking it
				continue;
			}
			removedRowCount += CheckGenericColumn(column);
		}
		
		Solver.DumpColumnCounts();
		return removedRowCount;
	}
	
	int CheckGenericColumn(DancingLinkHeader column)
	{
		var colName = column.Name.ToString();
		if (colName.StartsWith("cell"))
		{
			if (USE_ONLY_ONE_PATH_REACHES_CELL)
				return FilterCellColumnRows(column);
			return 0;
		}
		else if (colName.StartsWith("color"))
		{
			if (USE_COLOR_ALWAYS_GOES_THROUGH_CELL)
				return FilterColorColumns(column);
			return 0;
		}
		else
		{
			throw new Exception("Unknown column... wtf");
		}
	}
	
	///All possible paths going through this cell are one flow color
	int FilterCellColumnRows(DancingLinkHeader column)
	{
		DancingLinkHeader foundFlow = null;
		var validRows = new HashSet<DancingLinkNode>();
		foreach (var colRow in DancingLinks.EnumeratePath(column, x => x.South))
		{
			var flowColumn = FindFlowColumn(colRow);
			validRows.Add(flowColumn);
			
			if (foundFlow == null)
			{
				foundFlow = flowColumn.Header;
			}
			else if (foundFlow != flowColumn.Header)
			{
				//Does not apply to this filter
				//  that all the paths of this flow are using this cell
				return 0;
			}
		}
		
		
		//NOW find any other that are in it's column, but has no cells in validRows
		int cellsToKeepCount = 0, cellsToRemoveCount = 0;
		foreach (var flowRow in DancingLinks.EnumeratePath(foundFlow, x => x.South))
		{
			if (!validRows.Contains(flowRow))
			{
				//Remove this row!
				//"Remove this row!".Dump();
				++cellsToRemoveCount;
				DeleteRow(flowRow);
			}
			else
			{
				//Do not remove
				//"NOT removing row".Dump();
				++cellsToKeepCount;
			}
		}
		Util.HorizontalRun(true, foundFlow.Name, "\tRemove:", cellsToRemoveCount, "Keep:", cellsToKeepCount).Dump();
		
		return cellsToRemoveCount;
	}
	
	int FilterColorColumns(DancingLinkHeader column)
	{
		int totalCount = column.Count;
		var cellLookup = new Dictionary<DancingLinkHeader, int>();
		var cellTokeepLookup = new Dictionary<DancingLinkHeader, List<DancingLinkNode>>();
		
		Action<DancingLinkHeader, DancingLinkNode> incrementCellName = (x, y) =>
		{
			if (cellLookup.ContainsKey(x))
			{
				cellLookup[x]++;
				cellTokeepLookup[x].Add(y);
			}
			else
			{
				cellLookup.Add(x, 1);
				cellTokeepLookup.Add(x, new List<DancingLinkNode> { y });
			}
		};
		
		foreach (var colRow in DancingLinks.EnumeratePath(column, x => x.South))
		{
			foreach (var cell in DancingLinks.EnumeratePath(colRow, x => x.West))
			{
				incrementCellName(cell.Header, cell);
			}
		}
		
		//Now to check if any are always used
		var cellsToCleanout = new List<DancingLinkHeader>();
		//Lazy collection here...
		var allCellsToKeep = new HashSet<DancingLinkNode>();
		foreach (var kvp in cellLookup)
		{
			if (kvp.Value == totalCount)
			{
				cellsToCleanout.Add(kvp.Key);
				//allCellsToKeep.AddRange(cellTokeepLookup[kvp.Key]);
				cellTokeepLookup[kvp.Key].Each(x => allCellsToKeep.Add(x));
				("Cleaning up color column: " + kvp.Key.Name).Dump();
			}
			else
			{
				//"NOT cleaning up color column".Dump();
			}
		}
		
		int totalCellsRemoved = 0;
		
		//And remove any OTHER flows from this cell
		foreach (var cellColumn in cellsToCleanout)
		{
			//TODO idk what to do here now...
			//Um... go south and find any that DO NOT exist in allCellsToKeep?
			int cellsToKeepCount = 0, cellsToRemoveCount = 0;
			foreach (var toRemove in DancingLinks.EnumeratePath(cellColumn, x => x.South))
			{
				if (!allCellsToKeep.Contains(toRemove))
				{
					//"remove this row".Dump();
					++cellsToRemoveCount;
					DeleteRow(toRemove);
				}
				else
				{
					//"keeping this row".Dump();
					++cellsToKeepCount;
				}
			}
			totalCellsRemoved += cellsToRemoveCount;
			Util.HorizontalRun(true, cellColumn.Name, "\tRemove:", cellsToRemoveCount, "Keep:", cellsToKeepCount).Dump();
		}
		
		return totalCellsRemoved;
	}
}
public abstract class PostDancingLinksFilterBase
{
	public PostDancingLinksFilterBase(FlowSolver solver)
	{
		Solver = solver;
	}
	
	protected readonly FlowSolver Solver;
	
	protected DancingLinkNode FindFlowColumn(DancingLinkNode node)
	{
		var sp = node;
		while (!sp.Header.Name.ToString().StartsWith("color")) sp = sp.West;
		
		return sp;
	}
	
	protected void DeleteRow(DancingLinkNode node)
	{
		foreach (var toRemove in DancingLinks.EnumeratePath(node, x => x.East))
		{
			toRemove.Header.Count--;
			//Kill this reference for no reason...
			//toRemove.Header = null;
			
			//Span over this node
			if (toRemove.North.South != toRemove)
			{
				throw new Exception("FAILURE, redundant removal?");
			}
			if (toRemove.South.North != toRemove)
			{
				throw new Exception("FAILURE, redundant removal?");
			}
			
			if (toRemove.South == toRemove.North)
			{
				//Blah idk how an empty/new cell should work... or was this already removed or something
				throw new Exception("Zero rows");
			}
//			if (toRemove.North == toRemove.Header)
//			{
//				toRemove.Header.Count.Dump();
//				throw new Exception("header");
//			}
//			if (toRemove.South == toRemove.Header)
//			{
//				throw new Exception("header south");
//			}
			toRemove.North.South = toRemove.South;
			toRemove.South.North = toRemove.North;
			//toRemove.South = null;
			//toRemove.North = null;
		}
		//Remove 'node' cell too
		node.Header.Count--;
		node.North.South = node.South;
		node.South.North = node.North;
		//node.South = null;
		//node.North = null;
	}
	
	public abstract int RunFilter();
}




public class FlowSolver : DancingLinks
{
	public FlowSolver(BoardMask board, IEnumerable<PossiblePaths> pathsGenerators)
		: base(board.Width*board.Height + board.ColorCount)
	{
		SolutionFound = OutputSolution;
		
		Board = board;
		PathsGenerators = pathsGenerators;
		SetColumnNames();
	}
	
	BoardMask Board;
	IEnumerable<PossiblePaths> PathsGenerators;
	
	public IEnumerable<IEnumerable<int>> BuildDancingLinksRow(PossiblePaths paths)
	{
		return paths.FindPaths().Select(x => BuildDancingLinksRow(paths.PathId, x));
	}
	public IEnumerable<int> BuildDancingLinksRow(int flowId, IEnumerable<Coords> path)
	{
		//Cell indexes map directly
		foreach (var cell in path)
		{
			yield return Board.CoordsToCellId(cell);
		}
		//FlowNumbers are offset by cellCount
		yield return Board.Width*Board.Height + flowId;
	}
	
	private void SetColumnNames()
	{
		var cells = (Board.Width*Board.Height);
		for (int i = 0; i < cells; ++i)
		{
			Columns[i].Name = "cell" + i;
		}
//		for (int i = 0; i < Board.ColorCount; ++i)
//		{
//			Columns[cells + i].Name = "color" + i;
//		}
		for (int i = 0; i < Board.ColorCount; ++i)
		{
			var flowChar = Board.Flows.ToList()[i].Key;
			Columns[cells + i].Name = "color" + flowChar;
		}
	}
	
	public void BuildTableFull()
	{
		var generators = PathsGenerators.ToList();
		var totalCount = generators.Count;
		var current = 0;
		foreach (var paths in generators)
		{
			Util.Progress = current++ * 100 / generators.Count;
//			foreach (var DLCellList in paths.FindPaths()
//										.Select(x => BuildDancingLinksRow(x)))
			foreach (var DLCellList in BuildDancingLinksRow(paths))
			{
				AddRow(DLCellList);
			}
		}
		
		DumpColumnCounts();
		
		Util.Progress = null;
	}
	
	public void DumpColumnCounts()
	{
		var columnCounts = EnumeratePath(HEAD, (f) => f.East).Cast<DancingLinkHeader>().Select(x => x.Count);
		columnCounts.Dump("Column counts");
		columnCounts.RootMeanSquare(x => x).Dump("RMS of Column Count");
	}
	
	private bool OutputSolution(IEnumerable<DancingLinkNode> solution)
	{
		var pathStrings = GetFlowPathStrings(solution);
		var paths = FlowSplitter(pathStrings);
		
		var renderer = new FlowRenderer();
		renderer.Render(Board.Width, Board.Height, paths, Board.FlowColorFinder);
		
		//Always return true here... get all solutions
		return true;
	}
	private IEnumerable<string> GetFlowPathStrings(IEnumerable<DancingLinkNode> solution)
	{
		foreach (var s in solution)
		{
			var sp = s;
			while (!sp.Header.Name.ToString().StartsWith("color")) sp = sp.West;
			//yield return sp.Header.Name.ToString()
			//				+ " " + sp.East.Header.Name.ToString();
			var colorNode = sp;
			yield return colorNode.Header.Name.ToString();
			var sn = colorNode.East;
			while (sn != colorNode)
			{
				yield return sn.Header.Name.ToString();
				sn = sn.East;
			}
		}
	}
	IEnumerable<FlowPath> FlowSplitter(IEnumerable<string> pathStrings)
	{
		var buildingPath = new List<Coords>();
		string colorString = null;
		foreach (var p in pathStrings)
		{
			if (p.StartsWith("color"))
			{
				if (buildingPath.Count > 0)
				{
					var flowChar = colorString[5];
					yield return new FlowPath(flowChar, buildingPath);
					buildingPath = new List<Coords>();
				}
				colorString = p;
				continue;
			}
			
			var c = Board.CellIdToCoords(Int32.Parse(p.Replace("cell", "")));
			buildingPath.Add(c);
		}
		var flowChar2 = colorString[5];
		yield return new FlowPath(flowChar2, buildingPath);
	}
	
	public void DrawBoard()
	{
		var renderer = new FlowRenderer();
		renderer.Render(Board.Width, Board.Height,
			Board.Flows.Values.Select(x => new FlowPath(x.Flow, new [] { x.Start, x.End })), Board.FlowColorFinder);
	}
}
public class FlowPath
{
	public FlowPath(char flow, IEnumerable<Coords> path)
	{
		Flow = flow;
		Path = path;
	}
	
	public char Flow { get; private set; }
	public IEnumerable<Coords> Path { get; private set; }
}

public class FlowRenderer
{
	public void Render(int cellColumns, int cellRows, IEnumerable<FlowPath> paths, FlowColorFinder colorFinder = null)
	{
		//TODO do this better... pass in from board??
		colorFinder = colorFinder ?? new FlowColorFinder();
		
		var width = 200;
		var height = 200;
		var padding = 2;
		
		var cellWidth = (float)width / cellColumns;
		var cellHeight = (float)height / cellRows;
		
		using (var b = new Bitmap(width, height))
		using (var g = Graphics.FromImage(b))
		{
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			
			g.DrawRectangle(new Pen(Color.Black), 0, 0, width-1, height-1);
			
			foreach (var p in paths)
			{
				var color = colorFinder.GetColor(p.Flow); //GetColor(p.Flow);
				using (var brush = new SolidBrush(color))
				{
					foreach (var solutionCell in p.Path)
					{
						g.FillEllipse(brush,
							cellWidth * solutionCell.X + padding,
							cellHeight * solutionCell.Y + padding,
							cellWidth - 2*padding,
							cellHeight - 2*padding);
					}
				}
			}
			
			b.Dump();
		}
	}
}
public class FlowColorFinder
{
//	public FlowColorFinder(IEnumerable<FlowEndpoint> flows)
//		: this(flows.Select(x => x.Flow))
//	{
//	}
//	public FlowColorFinder(IEnumerable<FlowPath> flows)
//		: this(flows.Select(x => x.Flow))
//	{
//	}
	public void LoadAndSetFlows(Dictionary<char, FlowEndpoint> flows)
	{
		BuildColors(flows.Select(x => x.Value.Flow));
		
		foreach (var flow in flows)
		{
			flow.Value.Color = GetColor(flow.Key);
		}
	}
	
	void BuildColors(IEnumerable<char> flows)
	{
		//TODO pattern detection
		//  Contains all: rgby
		//  Sort, starts with abc
		//  Sort. starts with 123
		
		var flowsHere = flows.ToList();
		
		for (int i = flowsHere.Count-1; i >= 0; --i)
		{
			var f = flowsHere[i];
			Color? found = null;
			if (FlowKnownColors.ContainsKey(f))
				found = FlowKnownColors[f];
			
			if (found.HasValue)
			{
				FlowColorLookup.Add(f, found.Value);
				UsedColors.Add(found.Value);
				flowsHere.RemoveAt(i);
			}
		}
		
		//Any leftovers...
		var unusedColors = new Queue<Color>(ColorOrder.Except(UsedColors));
		for (int i = 0; i < flowsHere.Count;)
		{
			var f = flowsHere[i];
			if (unusedColors.Count > 0)
			{
				var found = unusedColors.Dequeue();
				FlowColorLookup.Add(f, found);
				UsedColors.Add(found);
				flowsHere.RemoveAt(i);
			}
			else
				break;
		}
		
		//Random colors
		foreach (var f in flowsHere)
		{
			var found = GetRandomColor();
			FlowColorLookup.Add(f, found);
			UsedColors.Add(found);
		}
	}
	
	Dictionary<char, Color> FlowColorLookup = new Dictionary<char, Color>();
	HashSet<Color> UsedColors = new HashSet<Color>();
	
	
	Random rand = new Random();
	Dictionary<char, Color> FlowKnownColors = new Dictionary<char, Color>
	{
		{ 'r', Color.Red },
		{ 'g', Color.Green },
		{ 'b', Color.Blue },
		{ 'y', Color.Yellow },
		{ 'o', Color.Orange },
		{ 'c', Color.Cyan },
		{ 'm', Color.Magenta },
		{ 'p', Color.Purple },
		{ 'w', Color.GhostWhite },
		//{ '', Color.Gray },
		{ 'l', Color.LimeGreen },
		//{ '', Color. },
	};
	Color[] ColorOrder = new []
	{
		//Unknown order:
		Color.Red,
		Color.Green,
		Color.Blue,
		Color.Yellow,
		Color.Orange, //4th
		Color.Cyan,
		Color.Magenta,
		Color.Purple,
		Color.GhostWhite,
		Color.Gray
	};
	Color GetRandomColor()
	{
		return Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
	}
	
	public Color GetColor(char flow)
	{
		var c = GetColor_determine(flow);
		Util.HorizontalRun(true, flow, c.Name).Dump();
		return c;
	}
	Color GetColor_determine(char flow)
	{
		if (FlowColorLookup.ContainsKey(flow))
			return FlowColorLookup[flow];
		return GetRandomColor();
	}
}








//Copied from SudokuSomething
public class DancingLinks
{
	public DancingLinkHeader HEAD { get; private set; }
	public List<DancingLinkHeader> Columns { get; private set; }
	
	Stack<DancingLinkNode> SolutionSet = new Stack<DancingLinkNode>();
	
	public DancingLinks(int ColumnCount)
	{
		HEAD = new DancingLinkHeader();
		Columns = new List<DancingLinkHeader>(ColumnCount);
		
		DancingLinkHeader current = HEAD;
		DancingLinkHeader next = null;
		for (int i = 0; i < ColumnCount; ++i)
		{
			//Create a header and insert it in the linked list
			next = new DancingLinkHeader();
			Columns.Add(next);
			current.East = next;
			next.West = current;
			//Set up/down loop around to this one
//			next.North = next;
//			next.South = next;
			current = next;
		}
		//Finish the wrapping around
		HEAD.West = next;
		next.East = HEAD;
	}
	
	public void AddRow(IEnumerable<int> cellIndexes)
	{
		StartAddRow();
		foreach (int cell in cellIndexes)
		{
			AddCellToRow(cell);
		}
		FinishAddRow();
	}
	
	bool addingRow = false;
	DancingLinkNode addRowStart = null;
	DancingLinkNode addRowPrev = null;
	public void StartAddRow()
	{
		if (addingRow) throw new InvalidConstraintException("Already adding row");
		if (addRowStart != null || addRowPrev != null)
		{
			throw new InvalidDataException("Unknown state..");
		}
		addingRow = true;
	}
	public void AddCellToRow(int ColumnIndex)
	{
		DancingLinkNode newNode = new DancingLinkNode();
		//Add to column
		Columns[ColumnIndex].InsertNorth(newNode);
		if (addRowStart == null)
		{
			addRowStart = newNode;
			addRowPrev = newNode;
		}
		else
		{
			addRowPrev.East = newNode;
			newNode.West = addRowPrev;
			addRowPrev = newNode;
		}
	}
	public void FinishAddRow()
	{
		if (addRowStart != null && addRowPrev != null)
		{
			//Wrap links around
			addRowPrev.East = addRowStart;
			addRowStart.West = addRowPrev;
			addRowPrev = null;
			addRowStart = null;
			addingRow = false;
		}
		else if (addRowStart == null && addRowPrev == null)
		{
			//Empty row
			addingRow = false;
		}
		else
		{
			throw new InvalidDataException("Unknown state..");
		}
	}
	
	
	public delegate bool SolutionCallback(IEnumerable<DancingLinkNode> solution);
	public SolutionCallback SolutionFound;
	
	public void Search()
	{
		Search(0);
	}
	private void Search(int depth)
	{
		ADDMESSAGE("Search-" + depth);
		if (HEAD.East == HEAD)
		{
			"SOLUTION FOUND".Dump();
			("Depth: " + depth).Dump();
			//SolutionSet.Dump(1);
			if (SolutionFound != null)
			{
				//TODO if this returns false, stop the searching...
				SolutionFound(SolutionSet.AsEnumerable());
			}
			ENDMESSAGE("Search-" + depth);
			return;
		}
		DancingLinkHeader column = FindColumnToCover();
		Cover(column);
		foreach (DancingLinkNode node in EnumeratePath(column, (f) => f.South))
		{
			ADDMESSAGE("Search-" + depth + "-#" + node);
			//Recurse this step
			SolutionSet.Push(node);
			foreach (DancingLinkNode child in EnumeratePath(node, (f) => f.East))
			{
				Cover(child.Header);
			}
			Search(depth + 1);
			//Backtrack this step
			/*node =*/ SolutionSet.Pop(); //Remove node from it, redundant?
			//column = node.Header; //Also redundant?
			foreach (DancingLinkNode child in EnumeratePath(node, (f) => f.West))
			{
				Uncover(child.Header);
			}
			ENDMESSAGE("Search-" + depth + "-#" + node);
		}
		Uncover(column);
		ENDMESSAGE("Search-" + depth);
		return;
	}
	public DancingLinkHeader FindColumnToCover()
	{
		ADDMESSAGE("FindColumnToCover");
		DancingLinkHeader targetColumn = null;
		int lowestSize = int.MaxValue;
		//I'm surprised this enumeration works...
		foreach (DancingLinkHeader column in EnumeratePath(HEAD, (f) => f.East))
		{
			ADDMESSAGE("FindColumnToCover-#" + column);
			if (column.Count < lowestSize)
			{
				ADDMESSAGE("FindColumnToCover-Set-" + column.Count + "-#" + column);
				lowestSize = column.Count;
				targetColumn = column;
				ENDMESSAGE("FindColumnToCover-Set-" + column.Count + "-#" + column);
			}
			ENDMESSAGE("FindColumnToCover-#" + column);
		}
		ENDMESSAGE("FindColumnToCover");
		return targetColumn;
	}
	public static IEnumerable<DancingLinkNode> EnumeratePath(DancingLinkNode start, Func<DancingLinkNode, DancingLinkNode> nextNode)
	{
		DancingLinkNode node = start;
		//if (includeStart) yield return start;
		//This is pretty crucial to work correctly...
		while ((node = nextNode(node)) != start && node != null)
		{
			yield return node;
		}
	}
	Stack<int> _CoveredIndexes = new Stack<int>();
	public void ResetCoverIndexes()
	{
		while (_CoveredIndexes.Count > 0)
		{
			Uncover(Columns[_CoveredIndexes.Pop()]);
		}
	}
	public void CoverIndexes(IEnumerable<int> columnIndexes)
	{
		foreach (int index in columnIndexes)
		{
			_CoveredIndexes.Push(index);
			Cover(Columns[index]);
		}
	}
	private void Cover(DancingLinkHeader column)
	{
		ADDMESSAGE("Cover-#" + column);
		if (column == null) throw new ArgumentNullException();
		
		column.East.West = column.West;
		column.West.East = column.East;
		foreach (DancingLinkNode columnCell in EnumeratePath(column, (f) => f.South))
		{
			foreach (DancingLinkNode cell in EnumeratePath(columnCell, (f) => f.East))
			{
				cell.North.South = cell.South;
				cell.South.North = cell.North;
				cell.Header.Count--;
			}
		}
		ENDMESSAGE("Cover-#" + column);
	}
	public void Uncover(DancingLinkHeader column)
	{
		ADDMESSAGE("Uncover-#" + column);
		foreach (DancingLinkNode columnCell in EnumeratePath(column, (f) => f.North))
		{
			foreach (DancingLinkNode cell in EnumeratePath(columnCell, (f) => f.West))
			{
				cell.Header.Count++;
				cell.North.South = cell;
				cell.South.North = cell;
			}
		}
		column.East.West = column;
		column.West.East = column;
		ENDMESSAGE("Uncover-#" + column);
	}
	
	///This should only output the non-covered cells
	public string ToStringOutput()
	{
		//"Outputting string".Dump();
		Dictionary<DancingLinkHeader, int> colMapping = new Dictionary<DancingLinkHeader, int>();
		//Rows in any order...
		List<BitArray> rows = new List<BitArray>();
		Dictionary<DancingLinkNode, int> rowMapping = new Dictionary<DancingLinkNode, int>();
		
		//Load columns hashtable
		int colIndex = 0;
		foreach (DancingLinkHeader col in EnumeratePath(HEAD, x => x.East))
		{
			colMapping.Add(col, colIndex++);
		}
		
		//Go through every cell
		foreach (DancingLinkHeader col in EnumeratePath(HEAD, x => x.East))
		{
			colIndex = colMapping[col];
			//TODO some shit is failing at this point
		//	col.Dump(2);
			foreach (DancingLinkNode node in EnumeratePath(col, x => x.South))
			{
				//node.Dump();
				if (!rowMapping.ContainsKey(node))
				{
					//"Adding row".Dump();
					//Need to recurse this row
					BitArray array = new BitArray(colMapping.Count);
					rowMapping.Add(node, colIndex);
					array[colIndex] = true;
					
					//node.Dump(1);
					foreach (DancingLinkNode cell in EnumeratePath(node, x => x.East))
					{
						//cell.Dump(1);
						int ci = colMapping[cell.Header];
						rowMapping.Add(cell, ci);
						array[ci] = true;
					}
					rows.Add(array);
				}
			}
		}
		
		//" -- String --".Dump();
		//Dump the rows I've collected
		StringBuilder sb = new StringBuilder();
		
		foreach (BitArray array in rows)
		{
			AppendBitArray(sb, array);
			sb.AppendLine();
		}
		return sb.ToString();
	}
	public static void AppendBitArray(StringBuilder sb, BitArray ba)
	{
		foreach (bool b in ba)
		{
			sb.Append(b ? '1' : '0');
		}
	}
}
public class DancingLinkNode
{
	public DancingLinkHeader Header { get; set; }
	public DancingLinkNode North { get; set; }
	public DancingLinkNode South { get; set; }
	
	public DancingLinkNode West { get; set; }
	public DancingLinkNode East { get; set; }
	
	
	public void InsertNorth(DancingLinkNode node)
	{
		Insert(node,
				n => n.North,
				(n,i) => n.North = i,
				//n => n.South,
				(n,i) => n.South = i);
	}
	public void InsertSouth(DancingLinkNode node)
	{
		Insert(node,
				n => n.South,
				(n,i) => n.South = i,
				//n => n.North,
				(n,i) => n.North = i);
	}
	public void InsertWest(DancingLinkNode node)
	{
		Insert(node,
				n => n.West,
				(n,i) => n.West = i,
				//n => n.East,
				(n,i) => n.East = i);
	}
	public void InsertEast(DancingLinkNode node)
	{
		Insert(node,
				n => n.East,
				(n,i) => n.East = i,
				//n => n.West,
				(n,i) => n.West = i);
	}
	///This probably is acutally a worse way to do it than just writing it normally
	public void Insert(DancingLinkNode node,
					Func<DancingLinkNode, DancingLinkNode> getPosition,
					Action<DancingLinkNode, DancingLinkNode> setPosition,
					//Func<DancingLinkNode, DancingLinkNode> getOpposite,
					Action<DancingLinkNode, DancingLinkNode> setOpposite)
	{
		setOpposite(node, this);
		setOpposite(getPosition(this) ?? this, node);
		setPosition(node, getPosition(this) ?? this);
		setPosition(this, node);
		//if (getOpposite(this) == null) setOpposite(this, node);
	}
	
	public override string ToString()
	{
		return GetHashCode().ToString();
	}
}
public class DancingLinkHeader : DancingLinkNode
{
	///Count is only needed to minimize the search tree
	public int Count { get; set; }
	public object Name { get; set; }
	
	///Or would it be better to just have something add a whole row at a time?
//	public void InsertInColumn(DancingLinkNode node)
//	{
////		node.South = this;
////		node.North = this.North;
////		this.North = node;
//		InsertNorth(node);
//		++this.Count;
//	}
	///Inserts the node directly north (when building you probably want this)
	//Why did i do 'new' here... and not override...
	public new void InsertNorth(DancingLinkNode node)
	{
		node.Header = this;
		base.InsertNorth(node);
		++this.Count;
	}
	///Inserts node directly south
	public new void InsertSouth(DancingLinkNode node)
	{
		node.Header = this;
		base.InsertSouth(node);
		++this.Count;
	}
}



static Stack<string> DEBUG_STACK = new Stack<string>();
static int DEBUG_COUNT = 0;
[Conditional("STACKON")]
[DebuggerStepThrough]
public static void ADDMESSAGE(string message)
{
	DEBUG_STACK.Push(message);
	
	++DEBUG_COUNT;
//	if (DEBUG_COUNT % 10 == 0)
//	{
//		(DEBUG_COUNT + " -- " + DEBUG_STACK.Count).Dump();
//	}
//	if (DEBUG_COUNT % 50 == 0)
//	{
//		DEBUG_STACK.Dump();
//		//throw new NoNullAllowedException();
//	}
	message.Dump();
	if (DEBUG_COUNT > 1000) throw new NoNullAllowedException();
}
[Conditional("STACKON")]
[DebuggerStepThrough]
public static void ENDMESSAGE(string message)
{
	string msg = DEBUG_STACK.Pop();
	if (msg != message) throw new DivideByZeroException();
}