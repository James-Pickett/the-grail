using System.Collections.Generic;

namespace HexMap
{
    public class HexCellPriorityQueue
    {
        private readonly List<HexCell> list = new List<HexCell>();
        private int minimum = int.MaxValue;

        public int Count { get; private set; }

        public void Enqueue(HexCell cell)
        {
            Count += 1;
            var priority = cell.SearchPriority;
            if (priority < minimum)
            {
                minimum = priority;
            }

            while (priority >= list.Count)
            {
                list.Add(item: null);
            }

            cell.NextWithSamePriority = list[index: priority];
            list[index: priority] = cell;
        }

        public HexCell Dequeue()
        {
            Count -= 1;
            for (; minimum < list.Count; minimum++)
            {
                var cell = list[index: minimum];
                if (cell != null)
                {
                    list[index: minimum] = cell.NextWithSamePriority;
                    return cell;
                }
            }

            return null;
        }

        public void Change(HexCell cell, int oldPriority)
        {
            var current = list[index: oldPriority];
            var next = current.NextWithSamePriority;
            if (current == cell)
            {
                list[index: oldPriority] = next;
            }
            else
            {
                while (next != cell)
                {
                    current = next;
                    next = current.NextWithSamePriority;
                }

                current.NextWithSamePriority = cell.NextWithSamePriority;
            }

            Enqueue(cell: cell);
            Count -= 1;
        }

        public void Clear()
        {
            list.Clear();
            Count = 0;
            minimum = int.MaxValue;
        }
    }
}