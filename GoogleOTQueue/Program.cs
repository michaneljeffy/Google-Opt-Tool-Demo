using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleOTQueue
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            var solver = new Solver("schedule_shifts");
            var num_nurses = 4;
            var num_shifts = 4;  // 班次数定为4，这样序号为0的班次表示是休息的班。
            var num_days = 7;

            // [START]
            // 创建班次变量
            var shifts = new Dictionary<(int, int), IntVar>();

            foreach (var j in Enumerable.Range(0, num_nurses))
            {
                foreach (var i in Enumerable.Range(0, num_days))
                {
                    // shifts[(j, i)]表示护士j在第i天的班次，可能的班次的编号范围是:[0, num_shifts)
                    shifts[(j, i)] = solver.MakeIntVar(0, num_shifts - 1, string.Format("shifts({0},{1})", j, i));
                }
            }

            // 将变量集合转成扁平化数组
            var shifts_flat = (from j in Enumerable.Range(0, num_nurses)
                               from i in Enumerable.Range(0, num_days)
                               select shifts[(j, i)]).ToArray();

            // 创建护士变量
            var nurses = new Dictionary<(int, int), IntVar>();

            foreach (var j in Enumerable.Range(0, num_shifts))
            {
                foreach (var i in Enumerable.Range(0, num_days))
                {
                    // nurses[(j, i)]表示班次j在第i天的当班护士，可能的护士的编号范围是:[0, num_nurses)
                    nurses[(j, i)] = solver.MakeIntVar(0, num_nurses - 1, string.Format("shift{0} day{1}", j, i));
                }
            }

            //定义关系
            foreach (var day in Enumerable.Range(0, num_days))
            {
                var nurses_for_day = (from j in Enumerable.Range(0, num_shifts)
                                      select nurses[(j, day)]).ToArray();
                foreach (var j in Enumerable.Range(0, num_nurses))
                {
                    var s = shifts[(j, day)];
                    // s.IndexOf(nurses_for_day)相当于nurses_for_day[s]
                    // 这里利用了s的值恰好是在nurses_for_day中对应nurse的编号
                    solver.Add(s.IndexOf(nurses_for_day) == j);
                }
            }

            // 满足每一天的当班护士不重复，每一天的班次不会出现重复的护士的约束条件
            // 同样每一个护士每天不可能同时轮值不同的班次
            foreach (var i in Enumerable.Range(0, num_days))
            {
                solver.Add((from j in Enumerable.Range(0, num_nurses)
                            select shifts[(j, i)]).ToArray().AllDifferent());
                solver.Add((from j in Enumerable.Range(0, num_shifts)
                            select nurses[(j, i)]).ToArray().AllDifferent());
            }

            // 满足每个护士在一周范围内只出现[5, 6]次
            foreach (var j in Enumerable.Range(0, num_nurses))
            {
                solver.Add((from i in Enumerable.Range(0, num_days)
                            select shifts[(j, i)] > 0).ToArray().Sum() >= 5);
                solver.Add((from i in Enumerable.Range(0, num_days)
                            select shifts[(j, i)] > 0).ToArray().Sum() <= 6);
            }

            // 创建一个工作的变量，works_shift[(i, j)]为True表示护士i在班次j一周内至少要有1次
            // BoolVar类型的变量最终取值是0或1，同样也表示了False或True
            var works_shift = new Dictionary<(int, int), IntVar>();

            foreach (var i in Enumerable.Range(0, num_nurses))
            {
                foreach (var j in Enumerable.Range(0, num_shifts))
                {
                    works_shift[(i, j)] = solver.MakeBoolVar(string.Format("nurse%d shift%d", i, j));
                }
            }

            foreach (var i in Enumerable.Range(0, num_nurses))
            {
                foreach (var j in Enumerable.Range(0, num_shifts))
                {
                    // 建立works_shift与shifts的关联关系
                    // 一周内的值要么为0要么为1，所以Max定义的约束是最大值，恰好也是0或1，1表示至少在每周轮班一天
                    solver.Add(works_shift[(i, j)] == (from k in Enumerable.Range(0, num_days)
                                                       select shifts[(i, k)].IsEqual(j)).ToArray().Max());
                }
            }

            // 对于每个编号不为0的shift, 满足至少每周最多同一个班次2个护士当班
            foreach (var j in Enumerable.Range(1, num_shifts - 1))
            {
                solver.Add((from i in Enumerable.Range(0, num_nurses)
                            select works_shift[(i, j)]).ToArray().Sum() <= 2);
            }

            // 满足中班或晚班的护士前一天或后一天也是相同的班次
            // 用nurses的key中Tuple类型第1个item的值表示shift为2或3
            // shift为1表示早班班次，shift为0表示休息的班次
            solver.Add(solver.MakeMax(nurses[(2, 0)] == nurses[(2, 1)], nurses[(2, 1)] == nurses[(2, 2)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 1)] == nurses[(2, 2)], nurses[(2, 2)] == nurses[(2, 3)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 2)] == nurses[(2, 3)], nurses[(2, 3)] == nurses[(2, 4)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 3)] == nurses[(2, 4)], nurses[(2, 4)] == nurses[(2, 5)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 4)] == nurses[(2, 5)], nurses[(2, 5)] == nurses[(2, 6)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 5)] == nurses[(2, 6)], nurses[(2, 6)] == nurses[(2, 0)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 6)] == nurses[(2, 0)], nurses[(2, 0)] == nurses[(2, 1)]) == 1);

            solver.Add(solver.MakeMax(nurses[(3, 0)] == nurses[(3, 1)], nurses[(3, 1)] == nurses[(3, 2)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 1)] == nurses[(3, 2)], nurses[(3, 2)] == nurses[(3, 3)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 2)] == nurses[(3, 3)], nurses[(3, 3)] == nurses[(3, 4)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 3)] == nurses[(3, 4)], nurses[(3, 4)] == nurses[(3, 5)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 4)] == nurses[(3, 5)], nurses[(3, 5)] == nurses[(3, 6)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 5)] == nurses[(3, 6)], nurses[(3, 6)] == nurses[(3, 0)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 6)] == nurses[(3, 0)], nurses[(3, 0)] == nurses[(3, 1)]) == 1);

            // 将变量集合设置为求解的目标，Solver有一系列的枚举值，可以指定求解的选择策略。
            var db = solver.MakePhase(shifts_flat, Solver.CHOOSE_FIRST_UNBOUND, Solver.ASSIGN_MIN_VALUE);


            // 创建求解的对象
            var solution = solver.MakeAssignment();
            solution.Add(shifts_flat);
            var collector = solver.MakeAllSolutionCollector(solution);
            //执行求解计算并显示结果

            solver.Solve(db, new[] { collector });
            Console.WriteLine("Solutions found: {0}", collector.SolutionCount());
            Console.WriteLine("Time: {0}ms", solver.WallTime());
            Console.WriteLine();

            // 显示一些随机的结果
            var a_few_solutions = new[] { 340, 2672, 7054 };

            foreach (var sol in a_few_solutions)
            {
                Console.WriteLine("Solution number {0}", sol);

                foreach (var i in Enumerable.Range(0, num_days))
                {
                    Console.WriteLine("Day {0}", i);
                    foreach (var j in Enumerable.Range(0, num_nurses))
                    {
                        Console.WriteLine("Nurse {0} assigned to task {1}", j, collector.Value(sol, shifts[(j, i)]));
                    }
                    Console.WriteLine();
                }       
            }

            Console.ReadLine();
        }
    }
}
