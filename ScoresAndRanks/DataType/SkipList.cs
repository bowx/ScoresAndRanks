using ScoresAndRanks.ExceptionHandler;
using System;
using System.Collections;
using static ScoresAndRanks.ExceptionHandler.ScoresAndRanksException;

namespace ScoresAndRanks.DataType
{
    public class SkipList<T> : ICollection<T>, ICollection where T : IComparable<T>, IHeadValueInit, new()
    {
        internal SkipListNode<T> head;
        internal int count;
        private SkipListNode<T> _topLeft;
        internal Random random;
        private int levels;
        private int maxLevels = int.MaxValue;
        //probability of level up, 2 is 1/2
        private readonly int _skiplist_p = 2;

        public SkipList()
        {
            //init head
            var item = new T();
            item.InitHead();
            head = new SkipListNode<T>(item, true, this);
            _topLeft = new SkipListNode<T>(item, true, this);
            AddBelow(_topLeft, head);
            levels = 1;
            count = 0;
            random = new Random();
        }

        //Key function of skiplist, Get index levels with probability
        //level 1 is 1/_skiplist_p, level 2 is 1/(_skiplist_p^2) ...
        private int getRandomLevels()
        {
            int newLevels = 0;
            while (random.Next(0, _skiplist_p) == 1 && newLevels < maxLevels)
            {
                newLevels++;
            }
            return newLevels;
        }

        public int Count
        {
            get { return count; }
        }

        public void Add(T item)
        {
            AddWithReturn(item);
        }

        public SkipListNode<T> AddWithReturn(T item)
        {
            int nodeLevels = getRandomLevels();
            //add new level if necessary
            CreateLevels(nodeLevels);
            SkipListNode<T> currentNode = _topLeft;
            //last node in index tree
            SkipListNode<T> lastIndexNode = null;
            SkipListNode<T> newNode = new SkipListNode<T>(item, this);
            int currentLevel = this.levels;
            //stack for nodes need to be recount
            Stack<SkipListNode<T>> recountIndexStack = new Stack<SkipListNode<T>>();
            while (currentLevel >= 0 && currentLevel != null)
            {
                //search in high level at first
                while (currentNode.Next != null)
                {
                    if (currentNode.Next != null && currentNode.Next.CompareTo(newNode) < 0)
                    {
                        currentNode = currentNode.Next;
                    }
                    else
                    {
                        break;
                    }
                }
                if (currentLevel > nodeLevels)
                {
                    currentNode = currentNode.Below;
                    currentLevel--;
                    continue;
                }
                //create new instance
                newNode = new SkipListNode<T>(item, this);
                AddAfter(currentNode, newNode);

                //push current node and new added node
                recountIndexStack.Push(currentNode);
                recountIndexStack.Push(newNode);

                if (lastIndexNode != null)
                {
                    AddBelow(lastIndexNode, newNode);
                }
                lastIndexNode = newNode;

                currentNode = currentNode.Below;
                currentLevel--;
            }
            var result = lastIndexNode;
            //recount
            SkipListNode<T> recountNode;
            SkipListNode<T> firstNode = null;
            while (recountIndexStack.TryPop(out recountNode))
            {
                recountNode.span = Recount(recountNode);
                firstNode = recountNode;
            }
            //update subCount from the first node in stack
            //add count for indexes
            //After go up the node subCount should +1, go left no change
            while (firstNode != null)
            {
                if (firstNode.Above != null)
                {
                    firstNode = firstNode.Above;
                    firstNode.span++;
                }
                else
                {
                    firstNode = firstNode.Previous;
                }
            }
            this.count++; //update count
            return result;

        }

        protected SkipListNode<T> Find(T item)
        {
            SkipListNode<T> findNode = _topLeft;
            SkipListNode<T> newNode = new SkipListNode<T>(item);

            while (findNode != null && findNode.Next != null)
            {
                if (findNode.Next != null && findNode.Next.CompareTo(newNode) < 0)
                {
                    findNode = findNode.Next;
                }
                else
                {
                    if (findNode.Next != null && findNode.Next.CompareTo(newNode) == 0)
                    {
                        findNode = findNode.Next;
                        break;
                    }
                    else
                    {
                        //scan down
                        findNode = findNode.Below;
                    }
                }
            }
            return findNode;
        }

        public bool Remove(T item)
        {
            var node = Find(item);
            return Remove(node);
        }

        public bool Remove(SkipListNode<T>? node)
        {
            //TODO vaildation node
            if (node == null)
            {
                return false;
            }
            else
            {
                //access to the top node
                while (node.Above != null)
                {
                    node = node.Above;
                }
                SkipListNode<T> prevNode = null;
                while (node != null)
                {
                    prevNode = node.Previous;
                    var belowNode = node.Below;

                    InternalRemoveNode(node);

                    //recount subCount for prev node
                    if (prevNode != null)
                    {
                        prevNode.span = Recount(prevNode);
                    }

                    node = belowNode;
                }
                //reduce count for indexes
                //After go up the node subCount should -1, go left no change
                if (prevNode != null)
                {
                    while (prevNode != null)
                    {
                        if (prevNode.Above != null)
                        {
                            prevNode = prevNode.Above;
                            prevNode.span--;
                        }
                        else
                        {
                            prevNode = prevNode.Previous;
                        }
                    }
                }

                //update counter
                this.count--;

                return true;
            }
        }

        private void InternalRemoveNode(SkipListNode<T> node)
        {
            if(node.prev != null) { node.prev.next = node.next; }
            if (node.next != null) { node.next.prev = node.prev; }
            //node.above is absolutely null
            if (node.below != null) { node.below.above = null; }
            node.Invalidate();
        }

        //recount the subCount for index node
        private int Recount(SkipListNode<T> node)
        {
            if (node.Below == null)
            {
                return node.span;
            }
            else
            {
                int count = 0;
                var downNode = node.Below;
                while (downNode != null)
                {
                    count += downNode.span;
                    downNode = downNode.Next;
                    if (downNode == null || downNode.Above != null)
                    {
                        break;
                    }
                }
                return count;
            }
        }

        //create empty new level index
        private void CreateLevels(int nodeLevel)
        {
            int newLevel = nodeLevel - this.levels;
            while (newLevel > 0)
            {
                int totalSpan = 0;
                var topLevelNode = _topLeft;
                while(topLevelNode != null)
                {
                    totalSpan += topLevelNode.span;
                    topLevelNode = topLevelNode.Next;
                }
                SkipListNode<T> newTopNode = new SkipListNode<T>(_topLeft.Value, true, this);
                newTopNode.span = totalSpan;
                newTopNode.below = _topLeft;
                _topLeft.above = newTopNode;
                _topLeft = newTopNode;

                newLevel--;
                this.levels++;
            }
        }

        private void AddAfter(SkipListNode<T> node, SkipListNode<T> newNode)
        {
            newNode.next = node.next;
            newNode.prev = node;
            if(node.next != null) node.next.prev = newNode;
            node.next = newNode;
        }

        private void AddBelow(SkipListNode<T> node, SkipListNode<T> newNode)
        {
            newNode.above = node;
            newNode.below = node.below;
            if(node.below != null) node.below.above = newNode;
            node.below = newNode;
        }

        public int GetIndex(SkipListNode<T> node)
        {
            //start from 1
            int index = 1;
            //only support the node at bottom
            if (node.IsIndex) throw new ScoresAndRanksException(ScoresAndRanksExceptionType.INDEX_NODE_NOT_SUPPORT);
            var pointNode = node;
            while (pointNode != null)
            {
                if (pointNode.Above != null)
                {
                    pointNode = pointNode.Above;
                    continue;
                }
                else if (pointNode.Previous == null)
                {
                    break;
                }
                else
                {
                    pointNode = pointNode.Previous;
                    index += pointNode.span;
                }
            }
            return index;
        }

        public SkipListNode<T> FindByIndex(int index)
        {
            if (index < 1 || index > this.Count) return null;
            SkipListNode<T> findNode = _topLeft;
            int indexFlag = index;
            while (findNode != null)
            {
                if (findNode.span < indexFlag)
                {
                    indexFlag -= findNode.span;
                    findNode = findNode.Next;
                    continue;
                }
                //rank flag is 1 and the point is down to the value link
                if (indexFlag == 1 && findNode.Below == null)
                {
                    break;
                }
                else
                {
                    //scan down
                    findNode = findNode.Below;
                }

            }
            return findNode;
        }

        #region Not used
        public bool Contains(T item)
        {
            return Find(item) != null;
        }
        public void Clear()
        {
            head.next = null;
            _topLeft = head.above;
            _topLeft.span = 0;
            _topLeft.above = null;
            count = 0;
            levels = 1;
            //Is it needed to invalidate all nodes?
        }
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }
        object ICollection.SyncRoot => this;

        public void CopyTo(T[] array, int arrayIndex)
        {
            //just using tha same method from LinkList
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

            if (arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "Argument out of range, Bigger than collection");
            }

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("Insufficient Space");
            }

            SkipListNode<T>? node = head;
            if (node != null)
            {
                do
                {
                    array[arrayIndex++] = node!.item;
                    node = node.next;
                } while (node != head);
            }
        }

        public void CopyTo(Array array, int index)
        {
            //just using tha same method from LinkList
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
            {
                throw new ArgumentException("Rank Multi Dim Not Supported", nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException("Non Zero Lower Bound", nameof(array));
            }

            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (array.Length - index < Count)
            {
                throw new ArgumentException("Insufficient Space");
            }

            T[]? tArray = array as T[];
            if (tArray != null)
            {
                CopyTo(tArray, index);
            }
            else
            {
                // No need to use reflection to verify that the types are compatible because it isn't 100% correct and we can rely
                // on the runtime validation during the cast that happens below (i.e. we will get an ArrayTypeMismatchException).
                object?[]? objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException("Incompatible ArrayType", nameof(array));
                }
                SkipListNode<T>? node = head;
                try
                {
                    if (node != null)
                    {
                        do
                        {
                            objects[index++] = node!.item;
                            node = node.next;
                        } while (node != head);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException("Incompatible ArrayType", nameof(array));
                }
            }
        }

        public IEnumerator<T> GetEnumerator() =>
            Count == 0 ? ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator() :
            GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);

        #endregion

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly SkipList<T> _list;
            private SkipListNode<T>? _node;
            private readonly int _version;
            private T? _current;
            private int _index;

            internal Enumerator(SkipList<T> list)
            {
                _list = list;
                _node = list.head;
                _current = default;
                _index = 0;
            }

            public T Current => _current!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _list.Count + 1))
                    {
                        throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
                    }

                    return Current;
                }
            }

            public bool MoveNext()
            {
                if (_node == null)
                {
                    _index = _list.Count + 1;
                    return false;
                }

                ++_index;
                _current = _node.item;
                _node = _node.next;
                if (_node == _list.head)
                {
                    _node = null;
                }
                return true;
            }

            void IEnumerator.Reset()
            {
                _current = default;
                _node = _list.head;
                _index = 0;
            }

            public void Dispose()
            {
            }

        }
    }
}
