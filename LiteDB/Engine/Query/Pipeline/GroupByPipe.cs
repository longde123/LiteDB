﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Implement query using GroupBy expression
    /// </summary>
    internal class GroupByPipe : BasePipe
    {
        public GroupByPipe(LiteEngine engine, LiteTransaction transaction, IDocumentLoader loader)
            : base(engine, transaction, loader)
        {
        }

        public override IEnumerable<BsonValue> Pipe(IEnumerable<BsonDocument> source, QueryPlan query)
        {
            // do includes in result before filter
            foreach (var path in query.IncludeBefore)
            {
                source = this.Include(source, path);
            }

            // filter results according expressions
            foreach (var expr in query.Filters)
            {
                source = this.Filter(source, expr);
            }

            // pipe: orderby using groupy expression
            source = this.OrderBy(source, query.GroupBy, query.GroupByOrder, 0, int.MaxValue);

            // do includes in result before filter
            foreach (var path in query.IncludeAfter)
            {
                source = this.Include(source, path);
            }

            // if expression contains more than 1 aggregate function must use in-memory collection
            // to avoid re-load all documents (and, in this case, it's not possible because mess with group order
            // this solution can cause leak memory - must be check better solution later
            var inMemory = query?.Select.AggregateCount > 1;

            // apply groupby
            var groups = this.GroupBy(source, query.GroupBy, inMemory);

            // now, get only first document from each group
            source = this.SelectGroupBy(groups, query.Select);

            // if contains OrderBy, must be run on end (after groupby select)
            if (query.OrderBy != null)
            {
                // pipe: orderby with offset+limit
                source = this.OrderBy(source, query.OrderBy, query.Order, query.Offset, query.Limit);
            }
            else
            {
                // pipe: apply offset (no orderby)
                if (query.Offset > 0) source = source.Skip(query.Offset);

                // pipe: apply limit (no orderby)
                if (query.Limit < int.MaxValue) source = source.Take(query.Limit);
            }

            // return document pipe
            return source;
        }

        /// <summary>
        /// Apply groupBy expression and transform results
        /// </summary>
        private IEnumerable<IEnumerable<BsonDocument>> GroupBy(IEnumerable<BsonDocument> source, BsonExpression expr, bool inMemory)
        {
            using (var enumerator = source.GetEnumerator())
            {
                var done = new Done { Running = enumerator.MoveNext() };

                while (done.Running)
                {
                    var group = YieldDocuments(enumerator, expr, done);

                    if (inMemory)
                    {
                        yield return group.ToList();
                    }
                    else
                    {
                        yield return group;
                    }
                }
            }
        }

        private IEnumerable<BsonDocument> YieldDocuments(IEnumerator<BsonDocument> source, BsonExpression expr, Done done)
        {
            var current = expr.Execute(source.Current, true).First();

            yield return source.Current;

            while (done.Running = source.MoveNext())
            {
                var key = expr.Execute(source.Current, true).First();

                if (key == current)
                {
                    yield return source.Current;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Transform groups of documents into single documents enumerable and apply select expression into group or return first document from each group
        /// </summary>
        private IEnumerable<BsonDocument> SelectGroupBy(IEnumerable<IEnumerable<BsonDocument>> groups, BsonExpression expr)
        {
            foreach (var group in groups)
            {
                // transfom group result if contains select expression
                if (expr != null)
                {
                    var result = expr.Execute(group, true);

                    var value = result.First();

                    if (value.IsDocument)
                    {
                        yield return value.AsDocument;
                    }
                    else
                    {
                        yield return new BsonDocument { ["expr"] = value };
                    }
                }
                else
                {
                    // if no select transform, return only first result
                    yield return group.First();
                }
            }
        }

        /// <summary>
        /// Bool inside a class to be used as "ref" parameter on ienumerable
        /// </summary>
        private class Done
        {
            public bool Running = false;
        }
    }
}