<Query Kind="Program">
  <NuGetReference>FubuCore</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Drawing</Namespace>
</Query>

//TODO support bridges

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

static bool USE_CAN_REACH_FILTERING = true;
static bool USE_CAN_REACH_ON_ALL_PATHS = true;
public void RunSolution()
{
//	var flowBoard = "---g-r-y--rb----g----by-------------";
//	var board = new BoardMask(6, 6, flowBoard);
	//var flowBoard = "b-b-"; //WORKS, pretty sure
	//var board = new BoardMask(2, 2, flowBoard);
//	var flowBoard = "b-------b"; //WORKS
//	var board = new BoardMask(3, 3, flowBoard);
	//var flowBoard = "a-ab----b";
	//var board = new BoardMask(3, 3, flowBoard);
	
//	//6x6 Mania, 108, not a good test board...
//	//  Well it is good for ones where ALL are valid, since the one with edge endpoints only has one possible path
//	//  But not good cause its only in one corner
//	var flowBoard = "byg-c-----g-------o--cm--by-r--or-m-";
//	var board = new BoardMask(6, 6, flowBoard);

//	//6x6 Mania, 110
//	var flowBoard = "brbogy-------r----------go---------y";
//	var board = new BoardMask(6, 6, flowBoard);
	
//	//6x6 Mania, 108
//	var flowBoard = "byg c     g       o  cm  by r  or m ";
//	var board = new BoardMask(6, 6, flowBoard);

	//6x6 Mania, 111
	var flowBoard = "b-----r---g--------gbr--------------";
	var board = new BoardMask(6, 6, flowBoard);
	
	var pathsGenerators = board.Flows.Values.Select((x, n) => new PossiblePaths(n, x.Start, x.End, board));
	
	var solver = new FlowSolver(board, pathsGenerators);
	solver.DrawBoard();
	
	solver.BuildTableFull();
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
		var a = new Coords(23, 53);
		var b = new Coords(23, 53);
		var c = new Coords(23, 54);
		
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
		
		var start = new Coords(0, 0);
		var end = new Coords(0, 1);
		
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
				board.MarkFilled(new Coords(x, y));
			}
			if (value == findPathsFor)
			{
				endpoints.Add(new Coords(x, y));
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
			
			if (step.Equals(End))
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
			
			if (c.Equals(End))
				yield return c;
			else if (!visited.Contains(c) && !Board.IsFilled(c))
			{
				yield return c;
			}
		}
	}
	IEnumerable<Coords> AllNextSteps(Coords current)
	{
		//current.Dump();
		//West
		if (current.X > 0)
			yield return new Coords(current.X - 1, current.Y);
		//East
		if (current.X < Board.Width-1)
			yield return new Coords(current.X + 1, current.Y);
		//North
		if (current.Y > 0)
			yield return new Coords(current.X, current.Y - 1);
		//South
		if (current.Y < Board.Height-1)
			yield return new Coords(current.X, current.Y + 1);
	}
	
	
	bool CountAsValidPath_OtherCanBeReached(Coords start, Coords end, IList<Coords> path)
	{
		//For now, eventually IsEdgeNode will find more paths
		//TODO above thing
		if (!USE_CAN_REACH_ON_ALL_PATHS
			&& (!IsEdgeNode(start)
			|| !IsEdgeNode(end)))
			return true;
		
		//Both are edge nodes
		
		var canReach = new FlowPassagesCanReach(Board, path);
		
		//TODO is there a better way to get the flows at this point?
		foreach (var flow in Board.Flows.Values)
		{
			if (flow.Start.Equals(start)
				|| flow.End.Equals(end)
				|| flow.Start.Equals(end)
				|| flow.End.Equals(start))
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
		//Everyone passes the tests here
		return true;
	}

	
	bool IsEdgeNode(Coords current)
	{
		//TODO also check if routes around it are blocked, and consider the edge node endpoints to be those
		//  Note DO NOT count the same color then...
		
		//fuck, find all the endpoints
		//  find where this line crosses it
		//  and the other endpoint should cross each line/column on the same way
		
		return current.Y == 0
			|| current.Y == Board.Height-1
			|| current.X == 0
			|| current.X == Board.Width-1;
	}
}
public class BoardMask : CellTranslator
{
	public BoardMask(int width, int height)
		: base(width)
	{
		Width = width;
		Height = height;
		_array = new BitArray(Width * Height);
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
	
	char[] EmptyCellChars = new [] { '-', ' ', '_' };
	public void LoadBoardDescription(string boardDescription)
	{
		var colorChars = new Dictionary<char, FlowEndpoint>();
		
		for (int x = 0; x < Width; ++x)
			for (int y = 0; y < Height; ++y)
			{
				var coords = new Coords(x, y);
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
		return new Coords(cellId % Width, cellId / Width);
	}
}
public struct Coords
{
	public Coords(int x, int y)
	{
		X = x;
		Y = y;
	}
	public readonly int X;
	public readonly int Y;
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
public class FlowPassagesCanReach
{
	public FlowPassagesCanReach(BoardMask board, IList<Coords> path)
	{
		Board = board;
		
		pathDescription = new PathDescription(path, Board.Width, Board.Height);
	}
	
	BoardMask Board { get; set; }
	
	PathDescription pathDescription;
	Dictionary<Coords, HashSet<Coords>> DynamicProgrammingLookup = new Dictionary<Coords, HashSet<Coords>>();
	
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
					DynamicProgrammingLookup.Values.Each(x => renderer.Render(6, 6, new [] { new FlowPath('a', x) }));
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
	
	IEnumerable<Coords> NextSteps(HashSet<Coords> visited, Coords current)
	{
		foreach (var c in AllNextSteps(current))
		{
			//c.Dump("step");
			//End.Dump("end");
			//Util.HorizontalRun(true, c.Equals(End), !visited.Contains(c), !Board.IsFilled(c)).Dump("compare");
			
			if (!visited.Contains(c)
				//&& !Board.IsFilled(c)
				&& !pathDescription.IsInPath(c))
			{
				yield return c;
			}
		}
	}
	IEnumerable<Coords> AllNextSteps(Coords current)
	{
		//current.Dump();
		//West
		if (current.X > 0)
			yield return new Coords(current.X - 1, current.Y);
		//East
		if (current.X < Board.Width-1)
			yield return new Coords(current.X + 1, current.Y);
		//North
		if (current.Y > 0)
			yield return new Coords(current.X, current.Y - 1);
		//South
		if (current.Y < Board.Height-1)
			yield return new Coords(current.X, current.Y + 1);
	}
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
		for (int i = 0; i < Board.ColorCount; ++i)
		{
			//TODO put the path in this?
			//aa
			Columns[cells + i].Name = "color" + i;
		}
	}
	
	public void BuildTableFull()
	{
		foreach (var paths in PathsGenerators)
		{
//			foreach (var DLCellList in paths.FindPaths()
//										.Select(x => BuildDancingLinksRow(x)))
			foreach (var DLCellList in BuildDancingLinksRow(paths))
			{
				AddRow(DLCellList);
			}
		}
		
		EnumeratePath(HEAD, (f) => f.East).Cast<DancingLinkHeader>().Select(x => x.Count).Dump("Column counts");
	}
	
	private bool OutputSolution(IEnumerable<DancingLinkNode> solution)
	{
		var pathStrings = GetFlowPathStrings(solution);
		var paths = FlowSplitter(pathStrings);
		
		var renderer = new FlowRenderer();
		renderer.Render(Board.Width, Board.Height, paths);
		
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
					yield return new FlowPath(colorString[5], buildingPath);
					buildingPath = new List<Coords>();
				}
				colorString = p;
				continue;
			}
			
			var c = Board.CellIdToCoords(Int32.Parse(p.Replace("cell", "")));
			buildingPath.Add(c);
		}
		yield return new FlowPath(colorString[5], buildingPath);
	}
	
	public void DrawBoard()
	{
		var renderer = new FlowRenderer();
		renderer.Render(Board.Width, Board.Height,
			Board.Flows.Values.Select(x => new FlowPath(x.Flow, new [] { x.Start, x.End })));
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
	public void Render(int cellColumns, int cellRows, IEnumerable<FlowPath> paths)
	{
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
				var color = GetColor(p.Flow);
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
	
	Random rand = new Random();
	Color GetColor(char flow)
	{
		return Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
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