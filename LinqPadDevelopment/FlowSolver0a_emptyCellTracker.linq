<Query Kind="Program" />

void Main()
{
	
}

public class NonDancingLinks
{
	public NonDancingLinks(int width, int height, int colorCount)
	{
		_emptyCells = new EmptyCellTracker(width*height);
		
	}
	
	EmptyCellTracker _emptyCells;
	
	
	public class CellNode<T>
	{
		public CellNode<T> North { get; set; }
		public CellNode<T> South { get; set; }
		public CellNode<T> West { get; set; }
		public CellNode<T> East { get; set; }
		
		public T Value { get; set; }
	}
}
public class EmptyCellTracker
{
	public EmptyCellTracker(int cellCount)
	{
		_bitArray = new BitArray(cellCount);
		
		for (int i = 0; i < cellCount; ++i)
		{
			var cell = new EmptyCell();
			_lookup.Add(i, _list.AddFirst(cell));
		}
	}
	
	BitArray _bitArray;
	LinkedList<EmptyCell> _list = new LinkedList<EmptyCell>();
	Dictionary<int, LinkedListNode<EmptyCell>> _lookup = new Dictionary<int, LinkedListNode<EmptyCell>>();
	
	class EmptyCell
	{
		
	}
	
	
	public bool IsFilled()
	{
		return _list.Count == 0;
	}
	public void MarkFilled(int cellId)
	{
		var node = _lookup[cellId];
		_list.Remove(node);
	}
	public void MarkEmpty(int cellId)
	{
		var node = _lookup[cellId];
		if (node.List != null)
			throw new Exception("marking empty cell empty again doesn't work");
		_list.AddFirst(node);
	}
}
