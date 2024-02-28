using Microsoft.AspNetCore.DataProtection.KeyManagement;
using ScoresAndRanks.Models;
using System.Collections.Concurrent;
using System.Globalization;

namespace ScoresAndRanks.DataType
{
    public class ConcurrentCustomerSortedList
    {
        private SortedList<IdScoreStruct, bool> _list;
        private ConcurrentDictionary<long, long> _idMapping;

        private static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        public ConcurrentCustomerSortedList() 
        {
            _idMapping = new ConcurrentDictionary<long, long>();
            _list = new SortedList<IdScoreStruct, bool>(
                Comparer<IdScoreStruct>.Create(
                    (x, y) =>
                    {
                        if (x.Score.Equals(y.Score)) return x.Id.CompareTo(y.Id);
                        return y.Score.CompareTo(x.Score);
                    }));
        }

        public struct IdScoreStruct
        { public long Id; public long Score; }

        /// <summary>
        /// Add or update with reader and writer lock
        /// </summary>
        /// <param name="idScore"></param>
        public void AddOrUpdate(IdScoreStruct idScore) 
        {
            try
            {
                rwLock.EnterUpgradeableReadLock();
                try
                {
                    if (!_idMapping.ContainsKey(idScore.Id))
                    {
                        //insert
                        rwLock.EnterWriteLock();
                        _idMapping.AddOrUpdate(idScore.Id, idScore.Score, (Id, Score) => { return idScore.Score; });
                        _list.Add(new IdScoreStruct
                        {
                            Id = idScore.Id,
                            Score = idScore.Score
                        }, true);
                    }
                    else 
                    {
                        //update
                        //get score and old key
                        var score = _idMapping[idScore.Id];
                        var oldKey = new IdScoreStruct { Id = idScore.Id, Score = score };
                        //calculate score
                        score += idScore.Score;
                        rwLock.EnterWriteLock();
                        _idMapping.AddOrUpdate(idScore.Id, idScore.Score, (Id, Score) => { return score; });
                        //key changed need to remove the old data
                        _list.Remove(oldKey);
                        _list.Add(new IdScoreStruct { Id = idScore.Id, Score = score }, true);
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
            finally { rwLock.ExitUpgradeableReadLock(); }
        }

        /// <summary>
        /// Get items by scope of rank with reader lock
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public SortedDictionary<int, IdScoreStruct> GetByRange(int start, int end)
        {
            try
            {
                rwLock.EnterReadLock();
                return FetchRange(ref start, ref end);

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
        public SortedDictionary<int, IdScoreStruct> GetByWindow(long currentId, int high, int low)
        {
            try
            {
                rwLock.EnterReadLock();
                //argument check
                high = high < 0 ? 0 : high;
                low = low < 0 ? 0 : low;
                var score = _idMapping[currentId];
                var count = _idMapping.Count;
                //Get rank from index
                var rank = _list.IndexOfKey(new IdScoreStruct { Id = currentId, Score = score }) + 1;
                //convert to start rank and end rank
                var ToHigh = rank - high > 1 ? rank - high : 1;
                var ToLow = rank + low > count ? count : rank + low;
                return FetchRange(ref ToHigh, ref ToLow);
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
        /// Get key for given index with reader lock
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IdScoreStruct GetKeyAtIndex(int index)
        {
            try
            {
                rwLock.EnterReadLock();
                return _list.GetKeyAtIndex(index);
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
        /// Get index of the key with reader lock
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexOfKey(IdScoreStruct key)
        {
            try
            {
                rwLock.EnterReadLock();
                return _list.IndexOfKey(key);
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
        /// Access IdScoreStruct with Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Struct with id and score</returns>
        public IdScoreStruct GetById(long id)
        {
            return new IdScoreStruct { Id = id, Score = _idMapping[id] };
        }

        /// <summary>
        /// Check if the id in the list
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsId(long id)
        {
            return _idMapping.ContainsKey(id);  
        }


        private SortedDictionary<int, IdScoreStruct> FetchRange(ref int start, ref int end)
        {
            var result = new SortedDictionary<int, IdScoreStruct>();
            //argument check
            if (end < start) throw new Exception("Invalid parameters.");
            if (start > _idMapping.Count) return result;
            if (start <= 0) start = 1;
            if (end > _idMapping.Count) end = _idMapping.Count;

            for (int i = start; i <= end; i++)
            {
                result.Add(i, _list.GetKeyAtIndex(i - 1));
            }
            return result;
        }

        
    }
}
