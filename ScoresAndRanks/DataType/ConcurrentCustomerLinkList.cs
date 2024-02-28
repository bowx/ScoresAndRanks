using System.Collections.Concurrent;
using System.Globalization;
using static ScoresAndRanks.DataType.ConcurrentCustomerSortedList;

namespace ScoresAndRanks.DataType
{
    public class ConcurrentCustomerLinkList
    {
        private LinkedList<ScoreRankModel> _linkList;
        //Dictionary for quick access link node by customer id
        private ConcurrentDictionary<long, LinkedListNode<ScoreRankModel>> _idMap;
        private static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        public class ScoreRankModel
        {
            public long Id;
            public long Score;
            public int Rank;
        }

        public ConcurrentCustomerLinkList()
        {
            _linkList = new LinkedList<ScoreRankModel>();
            _idMap = new ConcurrentDictionary<long, LinkedListNode<ScoreRankModel>>();
            
        }

        /// <summary>
        /// Add or update with reader and writer lock
        /// </summary>
        /// <param name="id"></param>
        /// <param name="score"></param>
        public void AddOrUpdate(long id, long score)
        {
            try
            {
                rwLock.EnterUpgradeableReadLock();

                try
                {
                    rwLock.EnterWriteLock();

                    if (!_idMap.ContainsKey(id))
                    {
                        var node = new LinkedListNode<ScoreRankModel>(new ScoreRankModel { Id = id, Score = score, Rank = 1 });
                        var currentNode = _linkList.Last;
                        //loop from back to front, and update the ramk at same time
                        InverseInsertNode(node, currentNode);
                    }
                    else
                    {
                        //update
                        var node = _idMap[id];
                        var itemStruct = node.Value;
                        itemStruct.Score += score;
                        node.Value = itemStruct;

                        if (score > 0)
                        {
                            var currentNode = node.Previous;
                            //break the links for the node temporarily
                            _linkList.Remove(node);
                            InverseInsertNode(node, currentNode);
                        }
                        else
                        {
                            var currentNode = node.Next;
                            _linkList.Remove(node);
                            ForwardInsertNode(node, currentNode);
                        }
                    }

                }
                catch (Exception)
                {
                    throw;
                }
                finally { rwLock.ExitWriteLock();}

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                rwLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Get items by scope of rank with reader lock
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<ScoreRankModel> GetByRange(int start, int end)
        {
            try
            {
                rwLock.EnterReadLock();
                var result = new List<ScoreRankModel>();
                if (end < start) throw new Exception("Invalid parameters.");
                if (start > _idMap.Count) return result;
                if (start <= 0) start = 1;

                var startNode = FetchNodeByRank(start);
                while (startNode != null && startNode.Value.Rank <= end)
                {
                    result.Add(startNode.Value);
                    startNode = startNode.Next;
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
        public List<ScoreRankModel> GetByWindow(long currentId, int high, int low)
        {
            try
            {
                rwLock.EnterReadLock();

                var node = _idMap[currentId];
                var result = new List<ScoreRankModel>();
                //upward search
                var prevNode = node.Previous;
                while (prevNode != null && high > 0)
                {
                    result.Add(new ScoreRankModel { Id = prevNode.Value.Id, Score = prevNode.Value.Score, Rank = prevNode.Value.Rank });
                    high--;
                    prevNode = prevNode.Previous;
                }
                result.Add(new ScoreRankModel { Id = node.Value.Id, Score = node.Value.Score, Rank = node.Value.Rank });
                //downward search
                var nextNode = node.Next;
                while (nextNode != null && low > 0)
                {
                    result.Add(new ScoreRankModel { Id = nextNode.Value.Id, Score = nextNode.Value.Score, Rank = nextNode.Value.Rank });
                    low--;
                    nextNode = nextNode.Next;
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
        public bool ContainsId(long id)
        {
            return _idMap.ContainsKey(id);
        }

        #region Methods for traversing
        private void InverseInsertNode(LinkedListNode<ScoreRankModel> node, LinkedListNode<ScoreRankModel>? currentNode)
        {
            while (currentNode != null)
            {
                //compare for score and id
                if (currentNode.Value.Score < node.Value.Score || (currentNode.Value.Score == node.Value.Score && currentNode.Value.Id > node.Value.Id))
                {
                    //the given node is rank ahead, and the current node rank should +1 
                    currentNode.Value.Rank++;
                    currentNode = currentNode.Previous;
                    continue;
                }
                else
                {
                    //the given node is just behind the current node, the rank of given node should be current node rank + 1
                    node.Value.Rank = currentNode.Value.Rank + 1;
                    _linkList.AddAfter(currentNode, node);
                    _idMap.AddOrUpdate(node.Value.Id, node, (Id, Node) => { return node; });
                    break;
                }
            }
            //Add to first
            if (null == currentNode)
            {
                node.Value.Rank = 1;
                _linkList.AddFirst(node);
                _idMap.AddOrUpdate(node.Value.Id, node, (Id, Node) => { return node; });
            }
        }

        private void ForwardInsertNode(LinkedListNode<ScoreRankModel> node, LinkedListNode<ScoreRankModel>? currentNode)
        {
            while (currentNode != null)
            {
                //compare for score and id
                if (currentNode.Value.Score > node.Value.Score || (currentNode.Value.Score == node.Value.Score && currentNode.Value.Id < node.Value.Id))
                {
                    //the given node is moving backward, the rank of current -1
                    currentNode.Value.Rank--;
                    currentNode = currentNode.Next;
                    continue;
                }
                else
                {
                    //find the position for given node, and the rank should be current -1
                    node.Value.Rank = currentNode.Value.Rank - 1;
                    _linkList.AddBefore(currentNode, node);
                    _idMap.AddOrUpdate(node.Value.Id, node, (Id, Node) => { return node; });
                    break;
                }
            }
            //Add to last
            if (null == currentNode)
            {
                node.Value.Rank = _idMap.Count;
                _linkList.AddLast(node);
                _idMap.AddOrUpdate(node.Value.Id, node, (Id, Node) => { return node; });
            }

        }

        private LinkedListNode<ScoreRankModel>? FetchNodeByRank(int rank)
        {
            //decides the direction by rank
            if (rank > (_idMap.Count / 2))
            {
                var node = _linkList.Last;
                while (node != null)
                {
                    if (node.Value.Rank == rank) break;
                    else
                    {
                        node = node.Previous;
                    }
                }
                return node;
            }
            else
            {
                var node = _linkList.First;
                while (node != null)
                {
                    if (node.Value.Rank == rank) break;
                    else
                    {
                        node = node.Next;
                    }
                }
                return node;
            }
        }
        #endregion



    }
}
