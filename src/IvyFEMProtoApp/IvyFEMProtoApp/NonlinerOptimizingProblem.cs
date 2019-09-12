using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SolverFoundation.Solvers;
using Microsoft.SolverFoundation.Services;

namespace IvyFEMProtoApp
{
    class NonlinerOptimizingProblem
    {
        public static void Solve(
            int paramCnt, double[] initialValues, double[] minValues, double[] maxValues, int maxIter,
            Func<INonlinearModel, int, ValuesByIndex, bool, object, double> callbackFunc, object callbackArg)
        {
            System.Diagnostics.Debug.Assert(paramCnt == initialValues.Length);

            NelderMeadSolver solver = new NelderMeadSolver();

            // 目的変数
            int objVId;
            solver.AddRow("obj", out objVId);
            solver.AddGoal(objVId, 0, true); // true: minimize

            for (int i = 0; i < paramCnt; i++)
            {
                // 変数
                int vId;
                solver.AddVariable("v" + i, out vId);

                // 範囲
                solver.SetBounds(vId, minValues[i], maxValues[i]);

                // 初期値
                solver.SetValue(vId, initialValues[i]);
            }

            // 目的関数をセット
            Func<INonlinearModel, int, ValuesByIndex, bool, double> callbackFunc2 =
                (model, rowVId, values, newValues) =>
                {
                    return callbackFunc(model, rowVId, values, newValues, callbackArg);
                };
            solver.FunctionEvaluator = callbackFunc2;

            // 中止関数
            Func<bool> abortFunc = () =>
            {
                return solver.IterationCount >= maxIter;
            };

            // 解く
            var solverParams = new NelderMeadSolverParams(abortFunc);
            INonlinearSolution solution = solver.Solve(solverParams);

            {
                System.Diagnostics.Debug.WriteLine("The Result is " + solution.Result + ".");
                System.Diagnostics.Debug.WriteLine("The minimium objective value is " +
                                  solution.GetValue(objVId) + ".");
                for (int i = 0; i < paramCnt; i++)
                {
                    string vKey = "v" + i;
                    int vId = solver.GetIndexFromKey(vKey);
                    System.Diagnostics.Debug.WriteLine(vKey + " = " +
                        solution.GetValue(vId));
                }
            }
        }
    }
}
