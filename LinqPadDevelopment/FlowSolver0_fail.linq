<Query Kind="Program" />

//FAIL ONE??
void Main()
{
	//6x6 mania 132, 4 colors
	var flowBoard = "---g-r-y--rb----g----by-------------";
	var flows = new BruteForceFlows(6, 6, flowBoard);
	
}

public class BruteForceFlows
{
	//const char EmptyCellChar = '-';
	char[] EmptyCellChars = new [] { '-', ' ', '_' };
	
	public BruteForceFlows(int width, int height, string boardDescription)
	{
		for (int x = 0; x < width; ++x)
			for (int y = 0; y < height; ++y)
			{
				var coords = new Coords(x, y);
				var value = boardDescription[y*width + x];
				
				CellNode<char> node;
				if (EmptyCellChars.Contains(value))
				{
					node = new CellNode<char>();
					_table.Add(coords, node);
				}
				else
				{
					node = new CellNode<char>(value, true);
					_table.Add(coords, node);
				}
				
				
				//Add pointers
				if (x > 0)
				{
					var west = _table[new Coords(x-1, y)];
					node.West = west;
					west.East = node;
				}
				if (y > 0)
				{
					var north = _table[new Coords(x, y-1)];
					node.North = north;
					north.South = node;
				}
			}
	}
	
	
	Dictionary<Coords, CellNode<char>> _table = new Dictionary<Coords, CellNode<char>>();
	
	public class CellNode<T>
	{
		public CellNode()
		{
			IsEmpty = true;
		}
		public CellNode(T value, bool endPoint)
		{
			Value = value;
			EndPoint = endPoint;
		}
		
		public CellNode<T> North { get; set; }
		public CellNode<T> South { get; set; }
		public CellNode<T> West { get; set; }
		public CellNode<T> East { get; set; }
		
		public bool IsEmpty { get; set; }
		public T Value { get; set; }
		public readonly bool EndPoint;
		
		public void SetValue(T value)
		{
			if (EndPoint)
				throw new Exception("Cannot change emdpoint value");
			Value = value;
			IsEmpty = false;
		}
		public void ClearValue()
		{
			if (EndPoint)
				throw new Exception("Cannot remove emdpoint value");
			Value = default(T);
			IsEmpty = true;
		}
		
		public IEnumerable<CellNode<T>> PossibleMoves()
		{
			if (North != null && North.IsEmpty)
			{
				yield return North;
			}
			if (South != null && South.IsEmpty)
			{
				yield return South;
			}
			if (West != null && West.IsEmpty)
			{
				yield return West;
			}
			if (East != null && East.IsEmpty)
			{
				yield return East;
			}
		}
	}
}
public class Coords : Tuple<int, int>
{
	public Coords(int x, int y)
		: base(x, y)
	{
	}
}