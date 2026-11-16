using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    // Assignment Phase 3. Implement as a binary min-heap.

    /// <summary>
    /// A minimum-priority queue implemented as a binary min-heap.
    ///
    /// Items with lower integer priority values should be returned before
    /// items with higher priority values.
    ///
    /// This class is intentionally incomplete in the assignment starter.
    /// </summary>
    public sealed class MinPriorityQueue<T>
    {
        // The list stores the physical representation of the binary heap.
        // Your implementation is responsible for preserving the min-heap invariant.
        private readonly List<Entry> _heap = new List<Entry>();

        /// <summary>
        /// Number of items currently stored in the queue.
        /// </summary>
        public int Count => _heap.Count;

        /// <summary>
        /// Adds an item with the supplied priority.
        /// Lower priority values are removed before higher priority values.
        /// </summary>
        public void Enqueue(T item, int priority)
        {
            // TODO: insert the new entry and restore the min-heap invariant.
            _heap.Add(new Entry(item, priority));

            
            SiftUp(Count - 1);
        }

        /// <summary>
        /// Returns the minimum-priority item without removing it.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is empty.
        /// </exception>
        public T Peek()
        {
            // TODO: return the current minimum without changing the queue.

            if(Count == 0)
            {
                throw new InvalidOperationException("There are no items in the heap."); 
            }

            return _heap[0].Item;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes and returns the minimum-priority item.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the queue is empty.
        /// </exception>
        public T Dequeue()
        {
            // TODO: remove the minimum item and restore the min-heap invariant.

            if (Count == 0)
            {
                throw new InvalidOperationException("There are no items in the heap.");
            }

            T originalItem = _heap[0].Item;

            (_heap[0], _heap[Count - 1]) = (_heap[Count - 1], _heap[0]);

            _heap.RemoveAt(Count - 1);

            if(Count > 0)         
                SiftDown(0);         

            return originalItem;
        }


        public void SiftUp(int index)
        {

            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;

                if (_heap[index].Priority >= _heap[parentIndex].Priority)
                    break;

                (_heap[index], _heap[parentIndex]) = (_heap[parentIndex], _heap[index]); 


                index = parentIndex;
            }
        }

        public void SiftDown(int index)
        {
            while (true)
            {
                int leftIndex = 2 * index + 1; 
                int rightIndex = 2 * index + 2;
                int smallest = index; 

                if(leftIndex < Count && _heap[leftIndex].Priority < _heap[smallest].Priority)
                    smallest = leftIndex;

                if(rightIndex < Count && _heap[rightIndex].Priority < _heap[smallest].Priority)
                    smallest = rightIndex;

                if (smallest == index)
                    break;

                (_heap[index], _heap[smallest]) = (_heap[smallest], _heap[index]);


                index = smallest; 
            }
        }
        /// <summary>
        /// Stores one item together with the priority used by the heap.
        /// </summary>
        private readonly struct Entry
        {
            public Entry(T item, int priority)
            {
                Item = item;
                Priority = priority;
            }

            public T Item { get; }
            public int Priority { get; }
        }
    }
}
