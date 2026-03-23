using System;
using System.Collections.Generic;

namespace Agents.Sample
{
    /// <summary>
    /// Defines the contract for building a summary report from a number sequence.
    /// </summary>
    public interface INumberReportAnalyzer
    {
        /// <summary>
        /// Generates a report containing aggregate metrics and grouped values.
        /// </summary>
        /// <param name="numbers">Input sequence to summarize.</param>
        /// <returns>A typed report object with status and computed metrics.</returns>
        NumberReport BuildReport(List<int> numbers);
    }

    /// <summary>
    /// Builds a summary report for a sequence of numbers.
    /// </summary>
    public class BadStyle : INumberReportAnalyzer
    {
        private const int WindowSize = 5;

        /// <summary>
        /// Generates a report containing aggregate metrics and grouped values.
        /// </summary>
        /// <param name="numbers">Input sequence to summarize.</param>
        /// <returns>A typed report object with status and computed metrics.</returns>
        public NumberReport BuildReport(List<int> numbers)
        {
            if (numbers == null)
            {
                return CreateMissingReport();
            }

            NumberAggregation aggregation = AggregateNumbers(numbers);
            return CreateCompletedReport(aggregation);
        }

        /// <summary>
        /// Returns a report with status "missing" when no input was provided.
        /// </summary>
        private static NumberReport CreateMissingReport()
        {
            return new NumberReport
            {
                Status = "missing"
            };
        }

        /// <summary>
        /// Iterates through all numbers and accumulates totals, extremes, and bucketed values.
        /// </summary>
        private static NumberAggregation AggregateNumbers(List<int> numbers)
        {
            NumberAggregation aggregation = new NumberAggregation();
            foreach (int value in numbers)
            {
                aggregation.Total += value;
                aggregation.Count++;
                UpdateExtrema(aggregation, value);
                AppendBuckets(aggregation, value);
            }

            return aggregation;
        }

        /// <summary>
        /// Updates minimum and maximum values on the aggregation for the given input.
        /// </summary>
        private static void UpdateExtrema(NumberAggregation aggregation, int value)
        {
            if (value > aggregation.Max)
            {
                aggregation.Max = value;
            }

            if (value < aggregation.Min)
            {
                aggregation.Min = value;
            }
        }

        /// <summary>
        /// Classifies a value into even/odd buckets, appends it to text, and updates sliding windows.
        /// </summary>
        private static void AppendBuckets(NumberAggregation aggregation, int value)
        {
            if (value % 2 == 0)
            {
                aggregation.Evens.Add(value);
            }
            else
            {
                aggregation.Odds.Add(value);
            }

            aggregation.ValuesAsText.Add(value.ToString());
            AppendFirstFive(aggregation.FirstFive, value);
            AppendLastFive(aggregation.LastFive, value);
        }

        /// <summary>
        /// Append a value to the first-five collection, up to 5 items.
        /// </summary>
        private static void AppendFirstFive(List<int> firstFive, int value)
        {
            if (firstFive.Count < WindowSize)
            {
                firstFive.Add(value);
            }
        }

        /// <summary>
        /// Append a value to the last-five collection, maintaining at most 5 items (sliding window).
        /// </summary>
        private static void AppendLastFive(List<int> lastFive, int value)
        {
            if (lastFive.Count == WindowSize)
            {
                lastFive.RemoveAt(0);
            }

            lastFive.Add(value);
        }

        /// <summary>
        /// Builds the final NumberReport from a completed aggregation.
        /// </summary>
        private static NumberReport CreateCompletedReport(NumberAggregation aggregation)
        {
            double average = aggregation.Count == 0 ? 0 : (double)aggregation.Total / aggregation.Count;

            return new NumberReport
            {
                Count = aggregation.Count,
                Total = aggregation.Total,
                Average = average,
                Min = aggregation.Min,
                Max = aggregation.Max,
                Evens = aggregation.Evens,
                Odds = aggregation.Odds,
                FirstFive = aggregation.FirstFive,
                LastFive = aggregation.LastFive,
                ValuesAsText = aggregation.ValuesAsText,
                GeneratedAt = DateTime.UtcNow,
                Status = "ok"
            };
        }
    }

    /// <summary>
    /// Represents the generated summary for a number sequence.
    /// </summary>
    public class NumberReport
    {
        /// <summary>Gets or sets the number of items processed.</summary>
        public int Count { get; set; }

        /// <summary>Gets or sets the sum of all values.</summary>
        public int Total { get; set; }

        /// <summary>Gets or sets the arithmetic mean of all values.</summary>
        public double Average { get; set; }

        /// <summary>Gets or sets the smallest value encountered.</summary>
        public int Min { get; set; } = int.MaxValue;

        /// <summary>Gets or sets the largest value encountered.</summary>
        public int Max { get; set; } = int.MinValue;

        /// <summary>Gets or sets the even numbers from the input.</summary>
        public List<int> Evens { get; set; } = new List<int>();

        /// <summary>Gets or sets the odd numbers from the input.</summary>
        public List<int> Odds { get; set; } = new List<int>();

        /// <summary>Gets or sets up to the first five values encountered.</summary>
        public List<int> FirstFive { get; set; } = new List<int>();

        /// <summary>Gets or sets the last five values encountered (sliding window).</summary>
        public List<int> LastFive { get; set; } = new List<int>();

        /// <summary>Gets or sets each value serialised as a string.</summary>
        public List<string> ValuesAsText { get; set; } = new List<string>();

        /// <summary>Gets or sets the UTC timestamp when the report was generated.</summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>Gets or sets the processing status: "ok" or "missing".</summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Internal accumulator used during report construction.
    /// </summary>
    internal class NumberAggregation
    {
        public int Total { get; set; }

        public int Count { get; set; }

        public int Min { get; set; } = int.MaxValue;

        public int Max { get; set; } = int.MinValue;

        public List<int> Evens { get; } = new List<int>();

        public List<int> Odds { get; } = new List<int>();

        public List<int> FirstFive { get; } = new List<int>();

        public List<int> LastFive { get; } = new List<int>();

        public List<string> ValuesAsText { get; } = new List<string>();
    }
}
