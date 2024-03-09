using ScoresAndRanks.ExceptionHandler;
using ScoresAndRanks.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using static ScoresAndRanks.DataType.ConcurrentCustomerSkipList;
using static ScoresAndRanks.ExceptionHandler.ScoresAndRanksException;

namespace ScoresAndRanks.DataType
{
    public class ConcurrentCustomerSkipList
    {

        private SkipList<ComparableCustomer> _skipList;
        //Dictionary for quick access link node by customer id
        private Dictionary<ulong, SkipListNode<ComparableCustomer>> _idMap;

        private static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();


        public class ComparableCustomer : Customer, IComparable<Customer>, IHeadValueInit
        {
            public int CompareTo(Customer? other)
            {
                var result = other.Score.CompareTo(this.Score);
                if (result == 0) return this.CustomerID.CompareTo(other.CustomerID);
                return result;
            }

            public void InitHead()
            {
                this.CustomerID = ulong.MinValue;
                this.Score = long.MaxValue;
            }
        }

        public ConcurrentCustomerSkipList()
        {
            _skipList = new SkipList<ComparableCustomer>();
            _idMap = new Dictionary<ulong, SkipListNode<ComparableCustomer>>();
        }

        /// <summary>
        /// Add or update with reader and writer lock
        /// </summary>
        /// <param name="id"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public long AddOrUpdate(ulong id, long score)
        {
            long result = 0;
            SkipListNode<ComparableCustomer> node = null;
            try
            {
                rwLock.EnterUpgradeableReadLock();
                bool isUpdate = _idMap.ContainsKey(id);
                //if id is exist and socre is 0, no update
                if (!isUpdate || score != 0)
                {
                    EnterWriteLock(() =>
                    {
                        //double check id exist
                        if (!_idMap.ContainsKey(id))
                        {
                            //insert
                            node = _skipList.AddWithReturn(new ComparableCustomer { CustomerID = id, Score = score });
                            _idMap.Add(node.Value.CustomerID, node);//AddOrUpdate(node.Value.CustomerID, node, (Id, Node) => { return node; });
                        }
                        else
                        {
                            //update
                            node = _idMap[id];
                            checked{ node.Value.Score += score;}
                            _skipList.Remove(node);
                            _idMap[id] = _skipList.AddWithReturn(node.Value);
                        }
                    });
                }
                else
                {
                    node = _idMap[id];
                }

                result = node.Value.Score;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                rwLock.ExitUpgradeableReadLock();
            }
            return result;
        }

        private void EnterWriteLock(Action action)
        {
            try
            {
                rwLock.EnterWriteLock();
                action();
            }
            catch (Exception)
            {

                throw;
            }
            finally 
            { 
                rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get items by scope of rank with reader lock
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<Customer> GetByRange(int start, int end)
        {

            var result = new List<Customer>();
            if (start > end) throw new ScoresAndRanksException(ScoresAndRanksExceptionType.END_LESS_THAN_START);
            if (start > _idMap.Count) return result;
            if (start <= 0) start = 1;
            try
            {
                rwLock.EnterReadLock();
                var startNode = _skipList.FindByIndex(start);
                for(int i = start; i <= end; i++)
                {
                    //don't show in the list if score is 0 or below
                    if (startNode.Value.Score <= 0) break;
                    result.Add(new Customer { 
                        CustomerID = startNode.Value.CustomerID,
                        Score = startNode.Value.Score,
                        Rank = i
                    });
                    startNode = startNode.Next;
                    if(startNode == null) break;
                }
                return result;

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get items by Id and a scope with reader lock
        /// </summary>
        /// <param name="currentId"></param>
        /// <param name="high"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public List<Customer> GetByWindow(ulong currentId, int high, int low)
        {
            try
            {
                rwLock.EnterReadLock();

                var node = _idMap[currentId];
                var rank = _skipList.GetIndex(node);
                var result = new List<Customer>();
                //upward search
                Stack<Customer> preStack = new Stack<Customer>();
                var prevNode = node.Previous;
                var prevRank = rank - 1;
                while (prevNode != null && !prevNode.isHead && high > 0)
                {
                    //don't show in the list if score is 0 or below
                    if (prevNode.Value.Score > 0){
                        preStack.Push(new Customer { CustomerID = prevNode.Value.CustomerID, Score = prevNode.Value.Score, Rank = prevRank });
                    }
                    high--;
                    prevRank--;
                    prevNode = prevNode.Previous;
                }
                Customer stackItem;
                while (preStack.TryPop(out stackItem))
                {
                    result.Add(stackItem);
                }
                //don't show in the list if score is 0 or below
                if (node.Value.Score > 0)
                {
                    result.Add(new Customer { CustomerID = node.Value.CustomerID, Score = node.Value.Score, Rank = rank });
                    //downward search
                    var nextNode = node.Next;
                    var nextRank = rank + 1;
                    while (nextNode != null && low > 0)
                    {
                        //don't show in the list if score is 0 or below
                        if(nextNode.Value.Score <= 0) { break; }
                        result.Add(new Customer { CustomerID = nextNode.Value.CustomerID, Score = nextNode.Value.Score, Rank = nextRank });
                        low--;
                        nextRank++;
                        nextNode = nextNode.Next;
                    }
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (rwLock.IsReadLockHeld) rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Check if the id is exist
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsId(ulong id)
        {
            return _idMap.ContainsKey(id);
        }


    }
}
