using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models
{
    public class TreeEntityLazyLoading<TEntity>
    {
        public int? Id { get; set; }
        public int? ParentId { get; set; }
        public TEntity Value { get; set; }
        public bool hasChildren { get; set; } = true;
    }

    public class TreeEntity<TEntity>
    {
        public int? Id { get; set; }
        public int? ParentId { get; set; }
        public TEntity Value { get; set; }
    }



    public class TreeCollectionLazyLoading<TEntity, TKey> : IEnumerable<TreeEntityLazyLoading<TEntity>>
    {        
        private readonly List<TEntity> _list;
        private readonly Func<TEntity, TKey> _keySelector;
        private readonly Func<TEntity, TKey> _parentSelector;
        public TreeCollectionLazyLoading(List<TEntity> list, Func<TEntity, TKey> keySelector, Func<TEntity, TKey> parentSelector)
        {
            _list = list;
            _keySelector = keySelector;
            _parentSelector = parentSelector;
        }

        public IEnumerator<TreeEntityLazyLoading<TEntity>> GetEnumerator()
        {
            var idx = new Dictionary<TKey, int>();
            for (int i = 0; i < _list.Count; i++)
            {
                TKey key = _keySelector(_list[i]);
                if (key != null)
                {
                    idx.Add(key, (i+1));
                }
            }

            for (int i = 0; i < _list.Count; i++) {
                TKey parentKey = _parentSelector(_list[i]);
                yield return
                    new TreeEntityLazyLoading<TEntity>()
                    {
                        Id = (i + 1),
                        ParentId = parentKey != null ? idx.ContainsKey(parentKey) ? idx[parentKey] : (int?)null
                                    : (int?)null,
                    Value = _list[i]
                };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }        

        public TreeDataSourceResult AsDataSource(DataSourceRequest request, int? id)
        {
            return this/*.Where(x=>x.ParentId==id)*/.ToList().ToTreeDataSourceResult(request,
                                org => org.Id,
                                org => org.ParentId,
                                org => id.HasValue ? org.ParentId== id : org.ParentId == null,
                                org =>org
                                );
        }

        public TreeDataSourceResult AsDataSource(DataSourceRequest request)
        {
            return this/*.Where(x=>x.ParentId==id)*/.ToList().ToTreeDataSourceResult(request,
                                org => org.Id,
                                org => org.ParentId,
                                org => org
                                );
        }
    }

    public class TreeCollection<TEntity, TKey> : IEnumerable<TreeEntity<TEntity>>
    {
        private readonly List<TEntity> _list;
        private readonly Func<TEntity, TKey> _keySelector;
        private readonly Func<TEntity, TKey> _parentSelector;
        public TreeCollection(List<TEntity> list, Func<TEntity, TKey> keySelector, Func<TEntity, TKey> parentSelector)
        {
            _list = list;
            _keySelector = keySelector;
            _parentSelector = parentSelector;
        }

        public IEnumerator<TreeEntity<TEntity>> GetEnumerator()
        {
            var idx = new Dictionary<TKey, int>();
            for (int i = 0; i < _list.Count; i++)
            {
                TKey key = _keySelector(_list[i]);
                if (key != null)
                {
                    idx.Add(key, (i + 1));
                }
            }

            for (int i = 0; i < _list.Count; i++)
            {
                TKey parentKey = _parentSelector(_list[i]);
                yield return
                    new TreeEntity<TEntity>()
                    {
                        Id = (i + 1),
                        ParentId = parentKey != null ? idx.ContainsKey(parentKey) ? idx[parentKey] : (int?)null
                                    : (int?)null,
                        Value = _list[i]
                    };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public TreeDataSourceResult AsDataSource(DataSourceRequest request)
        {
            return this/*.Where(x=>x.ParentId==id)*/.ToList().ToTreeDataSourceResult(request,
                                org => org.Id,
                                org => org.ParentId,
                                org => org
                                );
        }
    }
}