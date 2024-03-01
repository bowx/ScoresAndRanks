using ScoresAndRanks.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.PortableExecutable;
using static ScoresAndRanks.DataType.ConcurrentCustomerSkipList;

namespace ScoresAndRanks.DataType
{
    public class ConcurrentCustomerSkipList
    {
        private LinkedList<ScoreRankModel> _linkList;
        private LinkedList<ScoreRankModel> _topIndexList;
        //Dictionary for quick access link node by customer id
        private ConcurrentDictionary<ulong, LinkedListNode<ScoreRankModel>> _idMap;

        private static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        internal Random random;
        private int levels;
        private int size;
        private int maxLevels = int.MaxValue;
        //probability of level up, 2 is 1/2
        private readonly int _skiplist_p = 2;



        public class ScoreRankModel : IComparable<ScoreRankModel>
        {
            public LinkedListNode<ScoreRankModel> lowLevelNode = null;
            public LinkedListNode<ScoreRankModel> upLevelNode = null;
            //count of sub items
            public int subCount = 1;
            public bool isHeader = false;

            public ulong Id;
            public long Score;
            public int Rank;


            public int CompareTo(ScoreRankModel? other)
            {
                //header always be first
                if(other.isHeader) return 1;
                if(this.isHeader) return -1;
                var result = other.Score.CompareTo(this.Score);
                if(result == 0) return this.Id.CompareTo(other.Id);
                return result;
            }
        }


        public ConcurrentCustomerSkipList()
        {
            _linkList = new LinkedList<ScoreRankModel>();
            _topIndexList = new LinkedList<ScoreRankModel>();
            _linkList.AddFirst(new ScoreRankModel
            {
                Id = ulong.MinValue,
                Score = long.MaxValue,
                isHeader = true,
                subCount = 0,//the header node 's subCount in link should be 0 
                lowLevelNode = null
            });
            _topIndexList.AddFirst(new ScoreRankModel
            {
                Id = ulong.MinValue,
                Score = long.MaxValue,
                isHeader = true,
                subCount = 0,
                lowLevelNode = _linkList.First
            });
            _linkList.First.Value.upLevelNode = _topIndexList.First;
            _idMap = new ConcurrentDictionary<ulong, LinkedListNode<ScoreRankModel>>();
            levels = 1;
            size = 0;
            random = new Random();

        }


        protected int getRandomLevels()
        {
            int newLevels = 0;
            while (random.Next(0, _skiplist_p) == 1 && newLevels < maxLevels)
            {
                newLevels++;
            }
            return newLevels;
        }

        protected LinkedListNode<ScoreRankModel> Add(ScoreRankModel scoreRank)
        {
            int nodeLevels = getRandomLevels();
            //add new level if necessary
            CreateLevels(nodeLevels);
            LinkedListNode<ScoreRankModel> currentNode = _topIndexList.First;
            //last node in index tree
            LinkedListNode<ScoreRankModel> lastIndexNode = null;
            int currentLevel = this.levels;
            //stack for nodes need to be recount
            Stack<LinkedListNode<ScoreRankModel>> recountIndexStack = new Stack<LinkedListNode<ScoreRankModel>>();
            while (currentLevel >= 0 && currentLevel != null) 
            {
                //search in high level at first
                while (currentNode.Next != null)
                {
                    if (currentNode.Next != null && currentNode.Next.Value.CompareTo(scoreRank) < 0)
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
                    currentNode = currentNode.Value.lowLevelNode;
                    currentLevel--;
                    continue;
                }
                var newNode = new LinkedListNode<ScoreRankModel>(new ScoreRankModel { Id = scoreRank.Id, Score  = scoreRank.Score});
                currentNode.List.AddAfter(currentNode, newNode);
                //push current node and new added node
                recountIndexStack.Push(currentNode);
                recountIndexStack.Push(newNode);

                if (lastIndexNode != null)
                {
                    lastIndexNode.Value.lowLevelNode = newNode;
                    newNode.Value.upLevelNode = lastIndexNode;
                }
                lastIndexNode = newNode;

                currentNode = currentNode.Value.lowLevelNode;
                currentLevel--;
            }
            var result = lastIndexNode;
            //recount
            LinkedListNode<ScoreRankModel> recountNode;
            LinkedListNode<ScoreRankModel> firstNode = null;
            while (recountIndexStack.TryPop( out recountNode))
            {
                recountNode.Value.subCount = Recount(recountNode);
                firstNode = recountNode;
            }
            //update subCount from the first node in stack
            //add count for indexes
            //After go up the node subCount should +1, go left no change
            while (firstNode != null)
            {
                if (firstNode.Value.upLevelNode != null)
                {
                    firstNode = firstNode.Value.upLevelNode;
                    firstNode.Value.subCount++;
                }
                else
                {
                    firstNode = firstNode.Previous;
                }
            }

            this.size++; //update count
            return result;
        }

        protected LinkedListNode<ScoreRankModel> Find(ScoreRankModel scoreRank)
        {
            LinkedListNode<ScoreRankModel> findNode = _topIndexList.First;
            int rank = 0;

            while (findNode != null && findNode.Next != null)
            {
                if (findNode.Next != null && findNode.Next.Value.CompareTo(scoreRank) < 0)
                {
                    //
                    rank += findNode.Value.subCount;
                    findNode = findNode.Next;
                }
                else
                {
                    if (findNode.Next != null && findNode.Next.Value.Equals(scoreRank))
                    {
                        findNode = findNode.Next;
                        break;
                    }
                    else
                    {
                        //scan down
                        findNode = findNode.Value.lowLevelNode;
                    }
                }
            }
            //TODO update rank
            return findNode;
        }

        protected int GetRank(LinkedListNode<ScoreRankModel> node)
        {
            int rank = 1;
            //only support the node at bottom
            if (node.Value.lowLevelNode != null) throw new Exception("Invalid parameters.");
            var pointNode = node;
            while(pointNode != null)
            {
                if(pointNode.Value.upLevelNode != null)
                {
                    pointNode = pointNode.Value.upLevelNode;
                    continue;
                }
                else if(pointNode.Previous == null)
                {
                    break;
                }
                else
                {
                    pointNode = pointNode.Previous;
                    rank += pointNode.Value.subCount;
                }
            }
            return rank;
        }

        protected LinkedListNode<ScoreRankModel> FindByRank(int rank)
        {
            if (rank < 1 || rank > _linkList.Count) return null;
            LinkedListNode<ScoreRankModel> findNode = _topIndexList.First;
            int rankFlag = rank;
            while (findNode != null)
            {
                if(findNode.Value.subCount < rankFlag)
                {
                    rankFlag -= findNode.Value.subCount;
                    findNode = findNode.Next;
                    continue;
                }
                //rank flag is 1 and the point is down to the value link
                if(rankFlag == 1 && findNode.Value.lowLevelNode == null)
                {
                    break;
                }
                else
                {
                    //scan down
                    findNode = findNode.Value.lowLevelNode;
                }

            }
            return findNode;
        }

        protected bool Remove(LinkedListNode<ScoreRankModel> node)
        {
            if (node == null)
            {
                return false;
            }
            else
            {
                //access to the top node
                while (node.Value.upLevelNode != null)
                {
                    node = node.Value.upLevelNode;
                }
                LinkedListNode<ScoreRankModel> prevNode = null;
                while (node != null)
                {
                    prevNode = node.Previous;

                    node.List.Remove(node);
                    //remove the pointer for the lower node as well
                    if(node.Value.lowLevelNode != null)
                    {
                        node.Value.lowLevelNode.Value.upLevelNode = null;
                    }
                    //recount subCount for prev node
                    if(prevNode != null)
                    {
                        prevNode.Value.subCount = Recount(prevNode);
                    }

                    node = node.Value.lowLevelNode;
                }
                //reduce count for indexes
                //After go up the node subCount should -1, go left no change
                if(prevNode != null)
                {
                    while(prevNode != null)
                    {
                        if(prevNode.Value.upLevelNode != null)
                        {
                            prevNode = prevNode.Value.upLevelNode;
                            prevNode.Value.subCount--;
                        }
                        else
                        {
                            prevNode = prevNode.Previous;
                        }
                    }
                    
                    ////look forward for indexed node
                    //while (prevNode.Value.upLevelNode == null)
                    //{
                    //    prevNode = prevNode.Previous;
                    //}
                    ////go up though for all indexes
                    //while(prevNode.Value.upLevelNode != null)
                    //{
                    //    prevNode = prevNode.Value.upLevelNode;
                    //    prevNode.Value.subCount--;
                    //}
                }

                //update counter
                this.size--;

                return true;
            }
        }

        private int Recount(LinkedListNode<ScoreRankModel> node)
        {
            if (node.Value.lowLevelNode == null)
            {
                return node.Value.subCount;
            }
            else
            {
                int count = 0;
                var downNode = node.Value.lowLevelNode;
                while (downNode != null)
                {
                    count += downNode.Value.subCount;
                    downNode = downNode.Next;
                    if (downNode == null || downNode.Value.upLevelNode != null)
                    {
                        break;
                    }
                }
                return count;
            }
        }

        private void CreateLevels(int nodeLevel)
        {
            int newLevel = nodeLevel - this.levels;
            while(newLevel > 0)
            {
                LinkedList<ScoreRankModel> newIndexList = new LinkedList<ScoreRankModel>();
                int totalSubCount = 0;
                foreach(var node in _topIndexList)
                {
                    totalSubCount += node.subCount;
                }
                newIndexList.AddFirst(new ScoreRankModel
                {
                    Id = ulong.MinValue,
                    Score = long.MaxValue,
                    isHeader = true,
                    subCount = totalSubCount,
                    lowLevelNode = _topIndexList.First
                });
                _topIndexList.First.Value.upLevelNode = newIndexList.First;
                _topIndexList = newIndexList;

                newLevel--;
                this.levels++;
            }
        }


        /// <summary>
        /// Add or update with reader and writer lock
        /// </summary>
        /// <param name="id"></param>
        /// <param name="score"></param>
        public Customer AddOrUpdate(UInt64 id, long score)
        {
            try
            {
                Customer result = new Customer();
                result.CustomerID = id;
                rwLock.EnterUpgradeableReadLock();
                try
                {
                    rwLock.EnterWriteLock();

                    if (!_idMap.ContainsKey(id))
                    {
                        var node = Add(new ScoreRankModel { Id = id, Score = score });
                        _idMap.AddOrUpdate(node.Value.Id, node, (Id, Node) => { return node; });
                        result.Score = score;
                        result.Rank = GetRank(node);
                    }
                    else
                    {
                        //update
                        if(score != 0)
                        {
                            var node = _idMap[id];
                            //checked in case of overflow
                            checked
                            {
                                node.Value.Score += score;
                            }
                            this.Remove(node);
                            _idMap[id] = this.Add(node.Value);
                            result.Score = node.Value.Score;
                            result.Rank = GetRank(_idMap[id]);
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally { rwLock.ExitWriteLock();}
                return result;
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
        public List<Customer> GetByRange(int start, int end)
        {

            var result = new List<Customer>();
            if (end < start) throw new Exception("Invalid parameters.");
            if (start > _idMap.Count) return result;
            if (start <= 0) start = 1;
            try
            {
                rwLock.EnterReadLock();
                var startNode = FindByRank(start);
                for(int i = start; i <= end; i++)
                {
                    //don't show in the list if score is 0 or below
                    if (startNode.Value.Score <= 0) break;
                    result.Add(new Customer { 
                        CustomerID = startNode.Value.Id,
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
        public List<ScoreRankModel> GetByWindow(ulong currentId, int high, int low)
        {
            try
            {
                rwLock.EnterReadLock();

                var node = _idMap[currentId];
                var rank = GetRank(node);
                var result = new List<ScoreRankModel>();
                //upward search
                Stack<ScoreRankModel> preStack = new Stack<ScoreRankModel>();
                var prevNode = node.Previous;
                var prevRank = rank - 1;
                while (prevNode != null && !prevNode.Value.isHeader && high > 0)
                {
                    //don't show in the list if score is 0 or below
                    if (prevNode.Value.Score <= 0){
                        preStack.Push(new ScoreRankModel { Id = prevNode.Value.Id, Score = prevNode.Value.Score, Rank = prevRank });
                    }
                    high--;
                    prevRank--;
                    prevNode = prevNode.Previous;
                }
                ScoreRankModel stackItem;
                while (preStack.TryPop(out stackItem))
                {
                    result.Add(stackItem);
                }
                //don't show in the list if score is 0 or below
                if (node.Value.Score > 0)
                {
                    result.Add(new ScoreRankModel { Id = node.Value.Id, Score = node.Value.Score, Rank = rank });
                    //downward search
                    var nextNode = node.Next;
                    var nextRank = rank + 1;
                    while (nextNode != null && low > 0)
                    {
                        //don't show in the list if score is 0 or below
                        if(nextNode.Value.Score <= 0) { break; }
                        result.Add(new ScoreRankModel { Id = nextNode.Value.Id, Score = nextNode.Value.Score, Rank = nextRank });
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
