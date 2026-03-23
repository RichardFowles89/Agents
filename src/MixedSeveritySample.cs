using System.Collections.Generic;

namespace Agents.Sample
{
    /// <summary>
    /// Demonstrates mixed-severity architecture and style findings for agent validation.
    /// </summary>
    public class mixedSeveritySample
    {
        /// <summary>
        /// Builds a compact summary string from input numbers.
        /// </summary>
        /// <param name="numbers">Input numbers to summarize.</param>
        /// <returns>A summary containing count and average.</returns>
        public string BuildSummary(List<int> numbers)
        {
            if (numbers == null)
            {
                return "none";
            }

            int total = 0;
            foreach (int n in numbers)
            {
                total += n;
            }

            int avg = numbers.Count == 0 ? 0 : total / numbers.Count;
            string message = "count=" + numbers.Count + ",avg=" + avg;
            return message;
        }
    }
}
