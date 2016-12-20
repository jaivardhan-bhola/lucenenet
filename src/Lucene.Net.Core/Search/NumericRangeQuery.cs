using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;

    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements.  See the NOTICE file distributed with
         * this work for additional information regarding copyright ownership.
         * The ASF licenses this file to You under the Apache License, Version 2.0
         * (the "License"); you may not use this file except in compliance with
         * the License.  You may obtain a copy of the License at
         *
         *     http://www.apache.org/licenses/LICENSE-2.0
         *
         * Unless required by applicable law or agreed to in writing, software
         * distributed under the License is distributed on an "AS IS" BASIS,
         * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
         * See the License for the specific language governing permissions and
         * limitations under the License.
         */

    // for javadocs
    // for javadocs
    // for javadocs
    // for javadocs
    // for javadocs
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    // for javadocs

    /// <summary>
    /// <p>A <seealso cref="Query"/> that matches numeric values within a
    /// specified range.  To use this, you must first index the
    /// numeric values using <seealso cref="IntField"/>, {@link
    /// FloatField}, <seealso cref="LongField"/> or <seealso cref="DoubleField"/> (expert: {@link
    /// NumericTokenStream}).  If your terms are instead textual,
    /// you should use <seealso cref="TermRangeQuery"/>.  {@link
    /// NumericRangeFilter} is the filter equivalent of this
    /// query.</p>
    ///
    /// <p>You create a new NumericRangeQuery with the static
    /// factory methods, eg:
    ///
    /// <pre class="prettyprint">
    /// Query q = NumericRangeQuery.newFloatRange("weight", 0.03f, 0.10f, true, true);
    /// </pre>
    ///
    /// matches all documents whose float valued "weight" field
    /// ranges from 0.03 to 0.10, inclusive.
    ///
    /// <p>The performance of NumericRangeQuery is much better
    /// than the corresponding <seealso cref="TermRangeQuery"/> because the
    /// number of terms that must be searched is usually far
    /// fewer, thanks to trie indexing, described below.</p>
    ///
    /// <p>You can optionally specify a <a
    /// href="#precisionStepDesc"><code>precisionStep</code></a>
    /// when creating this query.  this is necessary if you've
    /// changed this configuration from its default (4) during
    /// indexing.  Lower values consume more disk space but speed
    /// up searching.  Suitable values are between <b>1</b> and
    /// <b>8</b>. A good starting point to test is <b>4</b>,
    /// which is the default value for all <code>Numeric*</code>
    /// classes.  See <a href="#precisionStepDesc">below</a> for
    /// details.
    ///
    /// <p>this query defaults to {@linkplain
    /// MultiTermQuery#CONSTANT_SCORE_AUTO_REWRITE_DEFAULT}.
    /// With precision steps of &lt;=4, this query can be run with
    /// one of the BooleanQuery rewrite methods without changing
    /// BooleanQuery's default max clause count.
    ///
    /// <br><h3>How it works</h3>
    ///
    /// <p>See the publication about <a target="_blank" href="http://www.panfmp.org">panFMP</a>,
    /// where this algorithm was described (referred to as <code>TrieRangeQuery</code>):
    ///
    /// <blockquote><strong>Schindler, U, Diepenbroek, M</strong>, 2008.
    /// <em>Generic XML-based Framework for Metadata Portals.</em>
    /// Computers &amp; Geosciences 34 (12), 1947-1955.
    /// <a href="http://dx.doi.org/10.1016/j.cageo.2008.02.023"
    /// target="_blank">doi:10.1016/j.cageo.2008.02.023</a></blockquote>
    ///
    /// <p><em>A quote from this paper:</em> Because Apache Lucene is a full-text
    /// search engine and not a conventional database, it cannot handle numerical ranges
    /// (e.g., field value is inside user defined bounds, even dates are numerical values).
    /// We have developed an extension to Apache Lucene that stores
    /// the numerical values in a special string-encoded format with variable precision
    /// (all numerical values like doubles, longs, floats, and ints are converted to
    /// lexicographic sortable string representations and stored with different precisions
    /// (for a more detailed description of how the values are stored,
    /// see <seealso cref="NumericUtils"/>). A range is then divided recursively into multiple intervals for searching:
    /// The center of the range is searched only with the lowest possible precision in the <em>trie</em>,
    /// while the boundaries are matched more exactly. this reduces the number of terms dramatically.</p>
    ///
    /// <p>For the variant that stores long values in 8 different precisions (each reduced by 8 bits) that
    /// uses a lowest precision of 1 byte, the index contains only a maximum of 256 distinct values in the
    /// lowest precision. Overall, a range could consist of a theoretical maximum of
    /// <code>7*255*2 + 255 = 3825</code> distinct terms (when there is a term for every distinct value of an
    /// 8-byte-number in the index and the range covers almost all of them; a maximum of 255 distinct values is used
    /// because it would always be possible to reduce the full 256 values to one term with degraded precision).
    /// In practice, we have seen up to 300 terms in most cases (index with 500,000 metadata records
    /// and a uniform value distribution).</p>
    ///
    /// <a name="precisionStepDesc"><h3>Precision Step</h3>
    /// <p>You can choose any <code>precisionStep</code> when encoding values.
    /// Lower step values mean more precisions and so more terms in index (and index gets larger). The number
    /// of indexed terms per value is (those are generated by <seealso cref="NumericTokenStream"/>):
    /// <p style="font-family:serif">
    /// &nbsp;&nbsp;indexedTermsPerValue = <b>ceil</b><big>(</big>bitsPerValue / precisionStep<big>)</big>
    /// </p>
    /// As the lower precision terms are shared by many values, the additional terms only
    /// slightly grow the term dictionary (approx. 7% for <code>precisionStep=4</code>), but have a larger
    /// impact on the postings (the postings file will have  more entries, as every document is linked to
    /// <code>indexedTermsPerValue</code> terms instead of one). The formula to estimate the growth
    /// of the term dictionary in comparison to one term per value:
    /// <p>
    /// <!-- the formula in the alt attribute was transformed from latex to PNG with http://1.618034.com/latex.php (with 110 dpi): -->
    /// &nbsp;&nbsp;<img src="doc-files/nrq-formula-1.png" alt="\mathrm{termDictOverhead} = \sum\limits_{i=0}^{\mathrm{indexedTermsPerValue}-1} \frac{1}{2^{\mathrm{precisionStep}\cdot i}}" />
    /// </p>
    /// <p>On the other hand, if the <code>precisionStep</code> is smaller, the maximum number of terms to match reduces,
    /// which optimizes query speed. The formula to calculate the maximum number of terms that will be visited while
    /// executing the query is:
    /// <p>
    /// <!-- the formula in the alt attribute was transformed from latex to PNG with http://1.618034.com/latex.php (with 110 dpi): -->
    /// &nbsp;&nbsp;<img src="doc-files/nrq-formula-2.png" alt="\mathrm{maxQueryTerms} = \left[ \left( \mathrm{indexedTermsPerValue} - 1 \right) \cdot \left(2^\mathrm{precisionStep} - 1 \right) \cdot 2 \right] + \left( 2^\mathrm{precisionStep} - 1 \right)" />
    /// </p>
    /// <p>For longs stored using a precision step of 4, <code>maxQueryTerms = 15*15*2 + 15 = 465</code>, and for a precision
    /// step of 2, <code>maxQueryTerms = 31*3*2 + 3 = 189</code>. But the faster search speed is reduced by more seeking
    /// in the term enum of the index. Because of this, the ideal <code>precisionStep</code> value can only
    /// be found out by testing. <b>Important:</b> You can index with a lower precision step value and test search speed
    /// using a multiple of the original step value.</p>
    ///
    /// <p>Good values for <code>precisionStep</code> are depending on usage and data type:
    /// <ul>
    ///  <li>The default for all data types is <b>4</b>, which is used, when no <code>precisionStep</code> is given.
    ///  <li>Ideal value in most cases for <em>64 bit</em> data types <em>(long, double)</em> is <b>6</b> or <b>8</b>.
    ///  <li>Ideal value in most cases for <em>32 bit</em> data types <em>(int, float)</em> is <b>4</b>.
    ///  <li>For low cardinality fields larger precision steps are good. If the cardinality is &lt; 100, it is
    ///  fair to use <seealso cref="Integer#MAX_VALUE"/> (see below).
    ///  <li>Steps <b>&gt;=64</b> for <em>long/double</em> and <b>&gt;=32</b> for <em>int/float</em> produces one token
    ///  per value in the index and querying is as slow as a conventional <seealso cref="TermRangeQuery"/>. But it can be used
    ///  to produce fields, that are solely used for sorting (in this case simply use <seealso cref="Integer#MAX_VALUE"/> as
    ///  <code>precisionStep</code>). Using <seealso cref="IntField"/>,
    ///  <seealso cref="LongField"/>, <seealso cref="FloatField"/> or <seealso cref="DoubleField"/> for sorting
    ///  is ideal, because building the field cache is much faster than with text-only numbers.
    ///  These fields have one term per value and therefore also work with term enumeration for building distinct lists
    ///  (e.g. facets / preselected values to search for).
    ///  Sorting is also possible with range query optimized fields using one of the above <code>precisionSteps</code>.
    /// </ul>
    ///
    /// <p>Comparisons of the different types of RangeQueries on an index with about 500,000 docs showed
    /// that <seealso cref="TermRangeQuery"/> in boolean rewrite mode (with raised <seealso cref="BooleanQuery"/> clause count)
    /// took about 30-40 secs to complete, <seealso cref="TermRangeQuery"/> in constant score filter rewrite mode took 5 secs
    /// and executing this class took &lt;100ms to complete (on an Opteron64 machine, Java 1.5, 8 bit
    /// precision step). this query type was developed for a geographic portal, where the performance for
    /// e.g. bounding boxes or exact date/time stamps is important.</p>
    ///
    /// @since 2.9
    ///
    /// </summary>
    public sealed class NumericRangeQuery<T> : MultiTermQuery
        where T : struct, IComparable<T> // best equiv constraint for java's number class
    {
        internal NumericRangeQuery(string field, int precisionStep, NumericType dataType, T? min, T? max, bool minInclusive, bool maxInclusive)
            : base(field)
        {
            if (precisionStep < 1)
            {
                throw new System.ArgumentException("precisionStep must be >=1");
            }
            this.precisionStep = precisionStep;
            this.DataType = dataType;
            this.min = min;
            this.max = max;
            this.MinInclusive = minInclusive;
            this.MaxInclusive = maxInclusive;
        }

        public override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            // very strange: java.lang.Number itself is not Comparable, but all subclasses used here are
            if (min.HasValue && max.HasValue && (min.Value).CompareTo(max.Value) > 0)
            {
                return TermsEnum.EMPTY;
            }
            return new NumericRangeTermsEnum(this, terms.Iterator(null));
        }

        /// <summary>
        /// Returns <code>true</code> if the lower endpoint is inclusive </summary>
        public bool IncludesMin()
        {
            return MinInclusive;
        }

        /// <summary>
        /// Returns <code>true</code> if the upper endpoint is inclusive </summary>
        public bool IncludesMax()
        {
            return MaxInclusive;
        }

        /// <summary>
        /// Returns the lower value of this range query </summary>
        public T? Min
        {
            get
            {
                return min;
            }
        }

        /// <summary>
        /// Returns the upper value of this range query </summary>
        public T? Max
        {
            get
            {
                return max;
            }
        }

        /// <summary>
        /// Returns the precision step. </summary>
        public int PrecisionStep
        {
            get
            {
                return precisionStep;
            }
        }

        public override string ToString(string field)
        {
            StringBuilder sb = new StringBuilder();
            if (!Field.Equals(field))
            {
                sb.Append(Field).Append(':');
            }
            return sb.Append(MinInclusive ? '[' : '{').Append((min == null) ? "*" : min.ToString()).Append(" TO ").Append((max == null) ? "*" : max.ToString()).Append(MaxInclusive ? ']' : '}').Append(ToStringUtils.Boost(Boost)).ToString();
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (!base.Equals(o))
            {
                return false;
            }
            if (o is NumericRangeQuery<T>)
            {
                var q = (NumericRangeQuery<T>)o;
                return ((q.min == null ? min == null : q.min.Equals(min)) && (q.max == null ? max == null : q.max.Equals(max)) && MinInclusive == q.MinInclusive && MaxInclusive == q.MaxInclusive && precisionStep == q.precisionStep);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash += precisionStep ^ 0x64365465;
            if (min != null)
            {
                hash += min.GetHashCode() ^ 0x14fa55fb;
            }
            if (max != null)
            {
                hash += max.GetHashCode() ^ 0x733fa5fe;
            }
            return hash + (Convert.ToBoolean(MinInclusive).GetHashCode() ^ 0x14fa55fb) + (Convert.ToBoolean(MaxInclusive).GetHashCode() ^ 0x733fa5fe);
        }

        // members (package private, to be also fast accessible by NumericRangeTermEnum)
        internal readonly int precisionStep;

        internal readonly NumericType DataType;
        internal readonly T? min, max;
        internal readonly bool MinInclusive, MaxInclusive;

        // used to handle float/double infinity correcty
        internal static readonly long LONG_NEGATIVE_INFINITY = NumericUtils.DoubleToSortableLong(double.NegativeInfinity);

        internal static readonly long LONG_POSITIVE_INFINITY = NumericUtils.DoubleToSortableLong(double.PositiveInfinity);
        internal static readonly int INT_NEGATIVE_INFINITY = NumericUtils.FloatToSortableInt(float.NegativeInfinity);
        internal static readonly int INT_POSITIVE_INFINITY = NumericUtils.FloatToSortableInt(float.PositiveInfinity);

        /// <summary>
        /// Subclass of FilteredTermsEnum for enumerating all terms that match the
        /// sub-ranges for trie range queries, using flex API.
        /// <p>
        /// WARNING: this term enumeration is not guaranteed to be always ordered by
        /// <seealso cref="Term#compareTo"/>.
        /// The ordering depends on how <seealso cref="NumericUtils#splitLongRange"/> and
        /// <seealso cref="NumericUtils#splitIntRange"/> generates the sub-ranges. For
        /// <seealso cref="MultiTermQuery"/> ordering is not relevant.
        /// </summary>
        private sealed class NumericRangeTermsEnum : FilteredTermsEnum
        {
            private readonly NumericRangeQuery<T> OuterInstance;

            internal BytesRef CurrentLowerBound, CurrentUpperBound;

            internal readonly LinkedList<BytesRef> RangeBounds = new LinkedList<BytesRef>();
            internal readonly IComparer<BytesRef> TermComp;

            internal NumericRangeTermsEnum(NumericRangeQuery<T> outerInstance, TermsEnum tenum)
                : base(tenum)
            {
                this.OuterInstance = outerInstance;
                switch (OuterInstance.DataType)
                {
                    case NumericType.LONG:
                    case NumericType.DOUBLE:
                        {
                            // lower
                            long minBound;
                            if (OuterInstance.DataType == NumericType.LONG)
                            {
                                minBound = (OuterInstance.min == null) ? long.MinValue : Convert.ToInt64(OuterInstance.min.Value);
                            }
                            else
                            {
                                Debug.Assert(OuterInstance.DataType == NumericType.DOUBLE);
                                minBound = (OuterInstance.min == null) ? LONG_NEGATIVE_INFINITY : NumericUtils.DoubleToSortableLong(Convert.ToDouble(OuterInstance.min.Value));
                            }
                            if (!OuterInstance.MinInclusive && OuterInstance.min != null)
                            {
                                if (minBound == long.MaxValue)
                                {
                                    break;
                                }
                                minBound++;
                            }

                            // upper
                            long maxBound;
                            if (OuterInstance.DataType == NumericType.LONG)
                            {
                                maxBound = (OuterInstance.max == null) ? long.MaxValue : Convert.ToInt64(OuterInstance.max);
                            }
                            else
                            {
                                Debug.Assert(OuterInstance.DataType == NumericType.DOUBLE);
                                maxBound = (OuterInstance.max == null) ? LONG_POSITIVE_INFINITY : NumericUtils.DoubleToSortableLong(Convert.ToDouble(OuterInstance.max));
                            }
                            if (!OuterInstance.MaxInclusive && OuterInstance.max != null)
                            {
                                if (maxBound == long.MinValue)
                                {
                                    break;
                                }
                                maxBound--;
                            }

                            NumericUtils.SplitLongRange(new LongRangeBuilderAnonymousInnerClassHelper(this), OuterInstance.precisionStep, minBound, maxBound);
                            break;
                        }

                    case NumericType.INT:
                    case NumericType.FLOAT:
                        {
                            // lower
                            int minBound;
                            if (OuterInstance.DataType == NumericType.INT)
                            {
                                minBound = (OuterInstance.min == null) ? int.MinValue : Convert.ToInt32(OuterInstance.min);
                            }
                            else
                            {
                                Debug.Assert(OuterInstance.DataType == NumericType.FLOAT);
                                minBound = (OuterInstance.min == null) ? INT_NEGATIVE_INFINITY : NumericUtils.FloatToSortableInt(Convert.ToSingle(OuterInstance.min));
                            }
                            if (!OuterInstance.MinInclusive && OuterInstance.min != null)
                            {
                                if (minBound == int.MaxValue)
                                {
                                    break;
                                }
                                minBound++;
                            }

                            // upper
                            int maxBound;
                            if (OuterInstance.DataType == NumericType.INT)
                            {
                                maxBound = (OuterInstance.max == null) ? int.MaxValue : Convert.ToInt32(OuterInstance.max);
                            }
                            else
                            {
                                Debug.Assert(OuterInstance.DataType == NumericType.FLOAT);
                                maxBound = (OuterInstance.max == null) ? INT_POSITIVE_INFINITY : NumericUtils.FloatToSortableInt(Convert.ToSingle(OuterInstance.max));
                            }
                            if (!OuterInstance.MaxInclusive && OuterInstance.max != null)
                            {
                                if (maxBound == int.MinValue)
                                {
                                    break;
                                }
                                maxBound--;
                            }

                            NumericUtils.SplitIntRange(new IntRangeBuilderAnonymousInnerClassHelper(this), OuterInstance.precisionStep, minBound, maxBound);
                            break;
                        }

                    default:
                        // should never happen
                        throw new System.ArgumentException("Invalid NumericType");
                }

                TermComp = Comparator;
            }

            private class LongRangeBuilderAnonymousInnerClassHelper : NumericUtils.LongRangeBuilder
            {
                private readonly NumericRangeTermsEnum OuterInstance;

                public LongRangeBuilderAnonymousInnerClassHelper(NumericRangeTermsEnum outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public override sealed void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
                {
                    OuterInstance.RangeBounds.AddLast(minPrefixCoded);
                    OuterInstance.RangeBounds.AddLast(maxPrefixCoded);
                }
            }

            private class IntRangeBuilderAnonymousInnerClassHelper : NumericUtils.IntRangeBuilder
            {
                private readonly NumericRangeTermsEnum OuterInstance;

                public IntRangeBuilderAnonymousInnerClassHelper(NumericRangeTermsEnum outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public override sealed void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
                {
                    OuterInstance.RangeBounds.AddLast(minPrefixCoded);
                    OuterInstance.RangeBounds.AddLast(maxPrefixCoded);
                }
            }

            internal void NextRange()
            {
                Debug.Assert(RangeBounds.Count % 2 == 0);

                CurrentLowerBound = RangeBounds.First.Value;
                RangeBounds.RemoveFirst();
                Debug.Assert(CurrentUpperBound == null || TermComp.Compare(CurrentUpperBound, CurrentLowerBound) <= 0, "The current upper bound must be <= the new lower bound");

                CurrentUpperBound = RangeBounds.First.Value;
                RangeBounds.RemoveFirst();
            }

            protected override BytesRef NextSeekTerm(BytesRef term)
            {
                while (RangeBounds.Count >= 2)
                {
                    NextRange();

                    // if the new upper bound is before the term parameter, the sub-range is never a hit
                    if (term != null && TermComp.Compare(term, CurrentUpperBound) > 0)
                    {
                        continue;
                    }
                    // never seek backwards, so use current term if lower bound is smaller
                    return (term != null && TermComp.Compare(term, CurrentLowerBound) > 0) ? term : CurrentLowerBound;
                }

                // no more sub-range enums available
                Debug.Assert(RangeBounds.Count == 0);
                CurrentLowerBound = CurrentUpperBound = null;
                return null;
            }

            protected override AcceptStatus Accept(BytesRef term)
            {
                while (CurrentUpperBound == null || TermComp.Compare(term, CurrentUpperBound) > 0)
                {
                    if (RangeBounds.Count == 0)
                    {
                        return AcceptStatus.END;
                    }
                    // peek next sub-range, only seek if the current term is smaller than next lower bound
                    if (TermComp.Compare(term, RangeBounds.First.Value) < 0)
                    {
                        return AcceptStatus.NO_AND_SEEK;
                    }
                    // step forward to next range without seeking, as next lower range bound is less or equal current term
                    NextRange();
                }
                return AcceptStatus.YES;
            }
        }
    }

    public static class NumericRangeQuery
    {
        /// <summary>
        /// Factory that creates a <code>NumericRangeQuery</code>, that queries a <code>long</code>
        /// range using the given <a href="#precisionStepDesc"><code>precisionStep</code></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<long> NewLongRange(string field, int precisionStep, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<long>(field, precisionStep, NumericType.LONG, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeQuery</code>, that queries a <code>long</code>
        /// range using the default <code>precisionStep</code> <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<long> NewLongRange(string field, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<long>(field, NumericUtils.PRECISION_STEP_DEFAULT, NumericType.LONG, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeQuery</code>, that queries a <code>int</code>
        /// range using the given <a href="#precisionStepDesc"><code>precisionStep</code></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<int> NewIntRange(string field, int precisionStep, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<int>(field, precisionStep, NumericType.INT, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeQuery</code>, that queries a <code>int</code>
        /// range using the default <code>precisionStep</code> <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<int> NewIntRange(string field, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<int>(field, NumericUtils.PRECISION_STEP_DEFAULT, NumericType.INT, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeQuery</code>, that queries a <code>double</code>
        /// range using the given <a href="#precisionStepDesc"><code>precisionStep</code></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>.
        /// <seealso cref="Double#NaN"/> will never match a half-open range, to hit {@code NaN} use a query
        /// with {@code min == max == Double.NaN}.  By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<double> NewDoubleRange(string field, int precisionStep, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<double>(field, precisionStep, NumericType.DOUBLE, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeQuery</code>, that queries a <code>double</code>
        /// range using the default <code>precisionStep</code> <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>.
        /// <seealso cref="Double#NaN"/> will never match a half-open range, to hit {@code NaN} use a query
        /// with {@code min == max == Double.NaN}.  By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<double> NewDoubleRange(string field, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<double>(field, NumericUtils.PRECISION_STEP_DEFAULT, NumericType.DOUBLE, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeQuery</code>, that queries a <code>float</code>
        /// range using the given <a href="#precisionStepDesc"><code>precisionStep</code></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>.
        /// <seealso cref="Float#NaN"/> will never match a half-open range, to hit {@code NaN} use a query
        /// with {@code min == max == Float.NaN}.  By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<float> NewFloatRange(string field, int precisionStep, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<float>(field, precisionStep, NumericType.FLOAT, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeQuery</code>, that queries a <code>float</code>
        /// range using the default <code>precisionStep</code> <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>.
        /// <seealso cref="Float#NaN"/> will never match a half-open range, to hit {@code NaN} use a query
        /// with {@code min == max == Float.NaN}.  By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<float> NewFloatRange(string field, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<float>(field, NumericUtils.PRECISION_STEP_DEFAULT, NumericType.FLOAT, min, max, minInclusive, maxInclusive);
        }
    }
}