using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTerminalEX {
    public static class Shared {
        public static int LevDist(string s1, string s2) {
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
