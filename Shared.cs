
namespace SubTerminalEX {

    /// <summary>
    /// Shared method
    /// </summary>
    public static class Shared {

        internal static int LevDist(string s1, string s2) {
            int[,] array = new int[s1.Length + 1, s2.Length + 1];
            for (int i = 0; i <= s1.Length; i++) {
                array[i, 0] = i;
            }
            for (int j = 0; j <= s2.Length; j++) {
                array[0, j] = j;
            }
            for (int k = 1; k <= s1.Length; k++) {
                for (int l = 1; l <= s2.Length; l++) {
                    int num = ((s1[k - 1] == s2[l - 1]) ? 0 : 1);
                    array[k, l] = Mathf.Min(Mathf.Min(array[k - 1, l] + 1, array[k, l - 1] + 1), array[k - 1, l - 1] + num);
                }
            }
            return array[s1.Length, s2.Length];
        }

        /// <summary>
        /// Get the most similar command that are closest to given command
        /// </summary>
        /// <param name="inputCommand">Target command to search</param>
        /// <returns>Command as string if found, otherwise return <see langword="null"/></returns>
        public static string? GetClosestCommand(string inputCommand) {
            int num = int.MaxValue;
            string? text = null;
            foreach (var (pair, num2) in from pair in TerminalCommandManager._cdict
                                         let num2 = LevDist(inputCommand, pair.Key)
                                         where num2 < num
                                         select (pair, num2)) {
                num = num2;
                text = pair.Key;
            }

            return text;
        }
    }
}