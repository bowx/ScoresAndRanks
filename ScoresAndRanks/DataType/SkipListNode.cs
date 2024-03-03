using System.Collections;
using System.Collections.Generic;

namespace ScoresAndRanks.DataType
{
    public class SkipListNode<T> : IComparable<SkipListNode<T>> where T : IComparable<T>, IHeadValueInit, new()
    {
        internal SkipList<T>? list;

        internal SkipListNode<T>? next;
        internal SkipListNode<T>? prev;
        internal SkipListNode<T>? above;
        internal SkipListNode<T>? below;
        internal T item;
        internal int span = 1;
        internal bool isHead;

        public SkipListNode(T value)
        {
            this.item = value;
        }

        public SkipListNode(T value, SkipList<T> list)
        {
            this.item = value;
            this.list = list;
        }

        internal SkipListNode(T value, bool isHead)
        {
            item = value;
            this.isHead = isHead;
            span = isHead ? 0 : 1;
        }

        internal SkipListNode(T value, bool isHead, SkipList<T> list)
        {
            item = value;
            this.isHead = isHead;
            span = isHead ? 0 : 1;
            this.list = list;
        }

        public T Value
        {
            get { return item; }
            set { item = value; }
        }

        public SkipList<T>? List
        {
            get { return list; }
        }

        public bool IsIndex
        {
            get { return this.below != null; } 
        }

        public SkipListNode<T>? Next
        {
            get { return next;  } 
        }

        public SkipListNode<T>? Previous
        {
            get { return prev; } 
        }

        public SkipListNode<T>? Above
        {
            get { return above; }
        }

        public SkipListNode<T>? Below
        {
            get { return below; }
        }

        public int CompareTo(SkipListNode<T>? other)
        {
            if (other == null) return -1;
            //head always be first
            if (other.isHead) return 1;
            if (this.isHead) return -1;
            return this.Value.CompareTo(other.Value);
        }

        internal void Invalidate()
        {
            list = null;
            next = null;
            prev = null;
            above = null;
            below = null;
        }
    }
}
