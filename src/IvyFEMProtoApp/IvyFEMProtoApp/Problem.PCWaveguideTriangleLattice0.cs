using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyFEM;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace IvyFEMProtoApp
{
    partial class Problem
    {
        public void PCWaveguideTriangleLatticeProblem0(MainWindow mainWindow)
        {
            double WaveguideWidth = 1.0;

            // 格子定数
            double latticeA = 0;
            // 周期構造距離
            double periodicDistance = 0;

            // フォトニック導波路
            // 考慮する波数ベクトルの最小値
            //double minWaveNum = 0.0;
            double minWaveNum = 0.5; // for latticeTheta = 60 r = 0.30a
            // 考慮する波数ベクトルの最大値
            //double maxWaveNum = 0.5;
            double maxWaveNum = 1.0; // for latticeTheta = 60 r = 0.30a

            // 空孔？
            //bool isAirHole = false; // dielectric rod
            bool isAirHole = true; // air hole
            // 周期を180°ずらす
            bool isShift180 = false; // for latticeTheta = 60 r = 0.30a air hole
            //bool isShift180 = true; // for latticeTheta = 45 r = 0.18a dielectric rod
            // ロッドの数(半分)
            //const int rodCntHalf = 5;
            const int rodCntHalf = 3;
            // 欠陥ロッド数
            const int defectRodCnt = 1;
            // 三角形格子の内角
            double latticeTheta = 60.0; // for latticeTheta = 60 r = 0.30a air hole
            // ロッドの半径
            double rodRadiusRatio = 0.30;
            // ロッドの比誘電率
            double rodEps = 2.76 * 2.76;
            // 1格子当たりの分割点の数
            //const int divCntForOneLattice = 9;
            const int divCntForOneLattice = 9;
            // ロッド円周の分割数
            const int rodCircleDiv = 12;
            // ロッドの半径の分割数
            const int rodRadiusDiv = 4;
            // 導波路不連続領域の長さ
            //const int rodCntDiscon = 4;
            const int rodCntDiscon = 2;

            // ロッドが1格子を超える？
            //bool isLargeRod = (rodRadiusRatio >= 0.25);
            bool isLargeRod = (rodRadiusRatio >= 0.5 * Math.Sin(latticeTheta * Math.PI / 180.0));
            // 格子の数
            int latticeCnt = rodCntHalf * 2 + defectRodCnt;
            // ロッド間の距離(Y方向)
            double rodDistanceY = WaveguideWidth / (double)latticeCnt;
            if (isLargeRod)
            {
                rodDistanceY = WaveguideWidth / (double)(latticeCnt - 1);
            }
            // 格子定数
            latticeA = rodDistanceY / Math.Sin(latticeTheta * Math.PI / 180.0);
            // ロッド間の距離(X方向)
            double rodDistanceX = rodDistanceY * 2.0 / Math.Tan(latticeTheta * Math.PI / 180.0);
            // 周期構造距離
            periodicDistance = rodDistanceX;
            // ロッドの半径
            double rodRadius = rodRadiusRatio * latticeA;
            // 導波路不連続領域の長さ
            double disconLength = rodDistanceX * rodCntDiscon;
            // 入出力導波路の周期構造部分の長さ
            double inputWgLength = rodDistanceX;
            // メッシュのサイズ
            double eLen = 1.05 * WaveguideWidth / (latticeCnt * divCntForOneLattice);

            // フォトニック結晶導波路の場合、a/λを規格化周波数とする
            double sFreq = 0.268;
            double eFreq = 0.282;
            int freqDiv = 20;

            // 最小屈折率
            double minEffN = 0.0;
            // 最大屈折率
            double maxEffN = 1.0;
            // air hole
            {
                minEffN = 0.0;
                maxEffN = Math.Sqrt(rodEps);
            }

            // μ0
            //double replacedMu0 = Constants.Mu0; // TEモード
            double replacedMu0 = Constants.Ep0; // TMモード

            // 媒質リスト作成
            double claddingP = 1.0;
            double claddingQ = 1.0;
            double coreP = 1.0;
            double coreQ = 1.0;
            if (isAirHole)
            {
                // 誘電体基盤 + 空孔(air hole)
                ///////////////////////////////////////
                // Note: IvyFEMではTEモードで実装されているが、係数を読み替えればTMモードにも適用できる
                ///////////////////////////////////////
                {
                    // TMモード
                    claddingP = 1.0 / rodEps;
                    claddingQ = 1.0;
                    coreP = 1.0 / 1.0;
                    coreQ = 1.0;
                }
                /*
                {
                    // TEモード
                    claddingP = 1.0;
                    claddingQ = rodEps;
                    coreP = 1.0;
                    coreQ = 1.0;
                }
                */
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }

            // 媒質リスト作成
            DielectricMaterial claddingMa = new DielectricMaterial
            {
                Epxx = claddingQ,
                Epyy = claddingQ,
                Epzz = claddingQ,
                Muxx = 1.0 / claddingP,
                Muyy = 1.0 / claddingP,
                Muzz = 1.0 / claddingP
            };
            DielectricMaterial coreMa = new DielectricMaterial
            {
                Epxx = coreQ,
                Epyy = coreQ,
                Epzz = coreQ,
                Muxx = 1.0 / coreP,
                Muyy = 1.0 / coreP,
                Muzz = 1.0 / coreP
            };

            // Cad
            CadObject2D cad2D = new CadObject2D();
            cad2D.IsSkipAssertValid = true; // AssertValidを無視する
            IList<uint> rodLoopIds = new List<uint>();
            IList<uint> inputWgRodLoopIds1 = new List<uint>();
            IList<uint> inputWgRodLoopIds2 = new List<uint>();
            int divCnt = 0;
            IList<uint> rodEIdsB1 = new List<uint>();
            IList<uint> rodEIdsB2 = new List<uint>();
            IList<uint> rodEIdsB3 = new List<uint>();
            IList<uint> rodEIdsB4 = new List<uint>();
            IList<uint> eIdsF1 = new List<uint>();
            IList<uint> eIdsF2 = new List<uint>();
            {
                //------------------------------------------------------------------
                // 図面作成
                //------------------------------------------------------------------
                {
                    IList<OpenTK.Vector2d> pts = new List<OpenTK.Vector2d>();
                    // 領域追加
                    pts.Add(new OpenTK.Vector2d(0.0, WaveguideWidth));
                    pts.Add(new OpenTK.Vector2d(0.0, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength + disconLength, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength * 2 + disconLength, 0.0));
                    pts.Add(new OpenTK.Vector2d(inputWgLength * 2 + disconLength, WaveguideWidth));
                    pts.Add(new OpenTK.Vector2d(inputWgLength + disconLength, WaveguideWidth));
                    pts.Add(new OpenTK.Vector2d(inputWgLength, WaveguideWidth));
                    uint lId1 = cad2D.AddPolygon(pts).AddLId;
                }
                // 入出力領域を分離
                uint eIdAdd1 = cad2D.ConnectVertexLine(3, 8).AddEId;
                uint eIdAdd2 = cad2D.ConnectVertexLine(4, 7).AddEId;

                // 入出力導波路の周期構造境界上の頂点を追加
                IList<double> ys = new List<double>();
                IList<double> rodys = new List<double>();
                IList<uint> rodVIIdsB1 = new List<uint>();
                IList<uint> rodVIdsB2 = new List<uint>();
                IList<uint> rodVIdsB3 = new List<uint>();
                IList<uint> rodVIdsB4 = new List<uint>();
                int outofAreaRodPtCntRowTop = 0;
                int outofAreaRodPtCntRowBottom = 0;
                // 境界上にロッドのある格子
                // 境界上のロッドの頂点
                for (int i = 0; i < rodCntHalf; i++)
                {
                    if ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 1 : 0)) continue;
                    double y0 = WaveguideWidth - i * rodDistanceY - 0.5 * rodDistanceY;
                    if (isLargeRod)
                    {
                        y0 += 0.5 * rodDistanceY;
                    }
                    if (y0 > (0.0 + Constants.PrecisionLowerLimit) &&
                        y0 < (WaveguideWidth - Constants.PrecisionLowerLimit))
                    {
                        rodys.Add(y0);
                    }
                    else
                    {
                        if (isLargeRod && i == 0)
                        {
                            outofAreaRodPtCntRowTop++;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                    }
                    for (int k = 1; k <= rodRadiusDiv; k++)
                    {
                        double y1 = y0 - k * rodRadius / rodRadiusDiv;
                        double y2 = y0 + k * rodRadius / rodRadiusDiv;
                        if (y1 > (0.0 + Constants.PrecisionLowerLimit) &&
                            y1 < (WaveguideWidth - Constants.PrecisionLowerLimit))
                        {
                            rodys.Add(y1);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        if (y2 > (0.0 + Constants.PrecisionLowerLimit) &&
                            y2 < (WaveguideWidth - Constants.PrecisionLowerLimit))
                        {
                            rodys.Add(y2);
                        }
                        else
                        {
                            if (isLargeRod && i == 0)
                            {
                                outofAreaRodPtCntRowTop++;
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                        }
                    }
                }
                for (int i = 0; i < rodCntHalf; i++)
                {
                    if (i % 2 == (isShift180 ? 1 : 0)) continue;
                    double y0 = rodDistanceY * rodCntHalf - i * rodDistanceY - 0.5 * rodDistanceY;
                    if (isLargeRod)
                    {
                        y0 -= 0.5 * rodDistanceY;
                    }
                    if (y0 > (0.0 + Constants.PrecisionLowerLimit) && y0 < (WaveguideWidth - Constants.PrecisionLowerLimit))
                    {
                        rodys.Add(y0);
                    }
                    else
                    {
                        if (isLargeRod && i == (rodCntHalf - 1))
                        {
                            outofAreaRodPtCntRowBottom++;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                    }
                    for (int k = 1; k <= rodRadiusDiv; k++)
                    {
                        double y1 = y0 - k * rodRadius / rodRadiusDiv;
                        double y2 = y0 + k * rodRadius / rodRadiusDiv;
                        if (y1 > (0.0 + Constants.PrecisionLowerLimit) && y1 < (WaveguideWidth - Constants.PrecisionLowerLimit))
                        {
                            rodys.Add(y1);
                        }
                        else
                        {
                            if (isLargeRod && i == (rodCntHalf - 1))
                            {
                                outofAreaRodPtCntRowBottom++;
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                        }
                        if (y2 > (0.0 + Constants.PrecisionLowerLimit) && y2 < (WaveguideWidth - Constants.PrecisionLowerLimit))
                        {
                            rodys.Add(y2);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                    }
                }
                foreach (double y_rod in rodys)
                {
                    ys.Add(y_rod);
                }
                // 境界上のロッドの外の頂点はロッドから少し離さないとロッドの追加で失敗するのでマージンをとる
                double radiusMargin = rodDistanceY * 0.01;
                // 境界上にロッドのある格子
                // ロッドの外
                for (int i = 0; i < rodCntHalf; i++)
                {
                    if ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 1 : 0)) continue;
                    for (int k = 1; k <= (divCntForOneLattice - 1); k++)
                    {
                        double divptY = WaveguideWidth - i * rodDistanceY - k * (rodDistanceY / divCntForOneLattice);
                        double minRodY =
                            WaveguideWidth - i * rodDistanceY - 0.5 * rodDistanceY - rodRadius - radiusMargin;
                        double maxRodY =
                            WaveguideWidth - i * rodDistanceY - 0.5 * rodDistanceY + rodRadius + radiusMargin;
                        if (isLargeRod)
                        {
                            divptY += rodDistanceY * 0.5;
                            if (divptY >= (WaveguideWidth - Constants.PrecisionLowerLimit)) continue;
                            minRodY += rodDistanceY * 0.5;
                            maxRodY += rodDistanceY * 0.5;
                        }
                        if (divptY < (minRodY - Constants.PrecisionLowerLimit) ||
                            divptY > (maxRodY + Constants.PrecisionLowerLimit))
                        {
                            ys.Add(divptY);
                        }
                    }
                }
                for (int i = 0; i < rodCntHalf; i++)
                {
                    if (i % 2 == (isShift180 ? 1 : 0)) continue;
                    for (int k = 1; k <= (divCntForOneLattice - 1); k++)
                    {
                        double divptY =
                            rodDistanceY * rodCntHalf - i * rodDistanceY - k * (rodDistanceY / divCntForOneLattice);
                        double minRodY =
                            rodDistanceY * rodCntHalf - i * rodDistanceY - 0.5 * rodDistanceY - rodRadius - radiusMargin;
                        double maxRodY =
                            rodDistanceY * rodCntHalf - i * rodDistanceY - 0.5 * rodDistanceY + rodRadius + radiusMargin;
                        if (isLargeRod)
                        {
                            divptY -= rodDistanceY * 0.5;
                            if (divptY <= (0.0 + Constants.PrecisionLowerLimit)) continue;
                            minRodY -= rodDistanceY * 0.5;
                            maxRodY -= rodDistanceY * 0.5;
                        }
                        if (divptY < (minRodY - Constants.PrecisionLowerLimit) ||
                            divptY > (maxRodY + Constants.PrecisionLowerLimit))
                        {
                            ys.Add(divptY);
                        }
                    }
                }

                // 境界上にロッドのない格子
                for (int i = 0; i < rodCntHalf; i++)
                {
                    if ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1)) continue;
                    for (int k = 0; k <= divCntForOneLattice; k++)
                    {
                        if (i == 0 && k == 0) continue;
                        double divptY = WaveguideWidth - i * rodDistanceY - k * (rodDistanceY / divCntForOneLattice);
                        double minUpperRodY =
                            WaveguideWidth - i * rodDistanceY + 0.5 * rodDistanceY - rodRadius - radiusMargin;
                        double maxLowerRodY = WaveguideWidth - (i + 1) * rodDistanceY - 0.5 * rodDistanceY + rodRadius + radiusMargin;
                        if (isLargeRod)
                        {
                            divptY += rodDistanceY * 0.5;
                            if (divptY >= (WaveguideWidth - Constants.PrecisionLowerLimit)) continue;
                            minUpperRodY += rodDistanceY * 0.5;
                            maxLowerRodY += rodDistanceY * 0.5;
                        }
                        bool isAddHalfRodRowTop = (isLargeRod
                            && ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0))));
                        if ((i != 0 || (i == 0 && isAddHalfRodRowTop))
                                && divptY >= (minUpperRodY - Constants.PrecisionLowerLimit))
                        {
                            continue;
                        }
                        if ((isShift180 || (!isShift180 && i != (rodCntHalf - 1)))
                            && divptY <= (maxLowerRodY + Constants.PrecisionLowerLimit))
                        {
                            continue;
                        }

                        ys.Add(divptY);
                    }
                }
                for (int i = 0; i < rodCntHalf; i++)
                {
                    if (i % 2 == (isShift180 ? 0 : 1)) continue;
                    for (int k = 0; k <= divCntForOneLattice; k++)
                    {
                        if (i == (rodCntHalf - 1) && k == divCntForOneLattice) continue;
                        double divptY =
                            rodDistanceY * rodCntHalf - i * rodDistanceY - k * (rodDistanceY / divCntForOneLattice);
                        double minUpperRodY =
                            rodDistanceY * rodCntHalf - i * rodDistanceY + 0.5 * rodDistanceY - rodRadius - radiusMargin;
                        double maxLowerRodY =
                            rodDistanceY * rodCntHalf - (i + 1) * rodDistanceY - 0.5 * rodDistanceY +
                            rodRadius + radiusMargin;
                        if (isLargeRod)
                        {
                            divptY -= rodDistanceY * 0.5;
                            if (divptY <= (0.0 + Constants.PrecisionLowerLimit)) continue;
                            minUpperRodY -= rodDistanceY * 0.5;
                            maxLowerRodY -= rodDistanceY * 0.5;
                        }
                        bool isAddHalfRodRowBottom = (isLargeRod
                            && ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0))));
                        if ((isShift180 || (!isShift180 && i != 0))
                            && divptY >= (minUpperRodY - Constants.PrecisionLowerLimit))
                        {
                            continue;
                        }
                        if ((i != (rodCntHalf - 1) || (i == (rodCntHalf - 1) && isAddHalfRodRowBottom))
                            && divptY <= (maxLowerRodY + Constants.PrecisionLowerLimit))
                        {
                            continue;
                        }

                        ys.Add(divptY);
                    }
                }
                // 欠陥部
                for (int i = 0; i <= (defectRodCnt * divCntForOneLattice); i++)
                {
                    if (!isShift180 && (i == 0 || i == (defectRodCnt * divCntForOneLattice))) continue;
                    double divptY = rodDistanceY * (rodCntHalf + defectRodCnt) - i * (rodDistanceY / divCntForOneLattice);
                    double minUpperRodY =
                        rodDistanceY * (rodCntHalf + defectRodCnt) + 0.5 * rodDistanceY - rodRadius - radiusMargin;
                    double maxLowerRodY =
                        rodDistanceY * rodCntHalf - 0.5 * rodDistanceY + rodRadius + radiusMargin;
                    if (isLargeRod)
                    {
                        divptY -= rodDistanceY * 0.5;
                        minUpperRodY -= rodDistanceY * 0.5;
                        maxLowerRodY -= rodDistanceY * 0.5;
                    }
                    if (isLargeRod && isShift180)
                    {
                        // for isLargeRod == true
                        if (divptY >= (minUpperRodY - Constants.PrecisionLowerLimit)
                                || divptY <= (maxLowerRodY + Constants.PrecisionLowerLimit)
                            )
                        {
                            continue;
                        }
                    }
                    ys.Add(divptY);
                }

                // 昇順でソート
                double[] yAry = ys.ToArray();
                Array.Sort(yAry);
                divCnt = yAry.Length + 1;

                // yAryは昇順なので、yAryの並びの順に追加すると境界1上を逆方向に移動することになる
                //  逆から追加しているのは、頂点によって新たに生成される辺に頂点を追加しないようにするため
                // 入力導波路 外側境界
                // 入力導波路 内部側境界
                // 出力導波路 外側境界
                // 出力導波路 内部側境界
                for (int boundaryIndex = 0; boundaryIndex < 4; boundaryIndex++)
                {
                    bool isInRod = false;
                    if (isLargeRod
                        && ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0))))
                    {
                        isInRod = true;
                    }

                    for (int i = 0; i < yAry.Length; i++)
                    {
                        uint eId = 0;
                        double x1 = 0.0;
                        double ptY = 0.0;
                        IList<uint> workrodEIdsB = null;
                        IList<uint> workrodVIdsB = null;
                        if (boundaryIndex == 0)
                        {
                            // 入力導波路 外側境界
                            eId = 1;
                            x1 = 0.0;
                            ptY = yAry[i];
                            workrodEIdsB = rodEIdsB1;
                            workrodVIdsB = rodVIIdsB1;
                        }
                        else if (boundaryIndex == 1)
                        {
                            // 入力導波路 内側境界
                            eId = 9;
                            x1 = inputWgLength;
                            ptY = yAry[yAry.Length - 1 - i];
                            workrodEIdsB = rodEIdsB2;
                            workrodVIdsB = rodVIdsB2;
                        }
                        else if (boundaryIndex == 2)
                        {
                            // 出力導波路 外側境界
                            eId = 5;
                            x1 = inputWgLength * 2 + disconLength;
                            ptY = yAry[yAry.Length - 1 - i];
                            workrodEIdsB = rodEIdsB3;
                            workrodVIdsB = rodVIdsB3;
                        }
                        else if (boundaryIndex == 3)
                        {
                            // 出力導波路 内側境界
                            eId = 10;
                            x1 = inputWgLength + disconLength;
                            ptY = yAry[yAry.Length - 1 - i];
                            workrodEIdsB = rodEIdsB4;
                            workrodVIdsB = rodVIdsB4;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }

                        var resAddVertex = cad2D.AddVertex(CadElementType.Edge, eId, new OpenTK.Vector2d(x1, ptY));
                        uint addVId = resAddVertex.AddVId;
                        uint addEId = resAddVertex.AddEId;
                        System.Diagnostics.Debug.Assert(addVId != 0);
                        System.Diagnostics.Debug.Assert(addEId != 0);
                        if (isInRod)
                        {
                            workrodEIdsB.Add(addEId);
                        }
                        bool contains = false;
                        foreach (double rodY in rodys)
                        {
                            if (Math.Abs(rodY - ptY) < Constants.PrecisionLowerLimit)
                            {
                                contains = true;
                                break;
                            }
                        }
                        if (contains)
                        {
                            workrodVIdsB.Add(addVId);

                            if (isLargeRod
                                && ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0))))
                            {
                                if ((workrodVIdsB.Count + outofAreaRodPtCntRowTop) % (rodRadiusDiv * 2 + 1) == 1)
                                {
                                    isInRod = true;
                                }
                                else if ((workrodVIdsB.Count + outofAreaRodPtCntRowTop) % (rodRadiusDiv * 2 + 1) == 0)
                                {
                                    isInRod = false;
                                }
                            }
                            else
                            {
                                if (workrodVIdsB.Count % (rodRadiusDiv * 2 + 1) == 1)
                                {
                                    isInRod = true;
                                }
                                else if (workrodVIdsB.Count % (rodRadiusDiv * 2 + 1) == 0)
                                {
                                    isInRod = false;
                                }
                            }
                        }
                        if (isLargeRod
                            && ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0))))
                        {
                            if (i == (yAry.Length - 1))
                            {
                                System.Diagnostics.Debug.Assert(isInRod == true);
                                workrodEIdsB.Add(eId);
                            }
                        }
                    }
                }

                int rodCntHalfB = (isShift180 ? (int)((rodCntHalf + 1) / 2) : (int)((rodCntHalf) / 2));
                if (!isLargeRod
                    || (isLargeRod &&
                           (isShift180 && (rodCntHalf % 2 == 0)) || (!isShift180 && (rodCntHalf % 2 == 1))
                       )
                    )
                {
                    System.Diagnostics.Debug.Assert(rodVIIdsB1.Count == rodCntHalfB * 2 * (rodRadiusDiv * 2 + 1));
                    System.Diagnostics.Debug.Assert(rodVIdsB2.Count == rodCntHalfB * 2 * (rodRadiusDiv * 2 + 1));
                    System.Diagnostics.Debug.Assert(rodVIdsB3.Count == rodCntHalfB * 2 * (rodRadiusDiv * 2 + 1));
                    System.Diagnostics.Debug.Assert(rodVIdsB4.Count == rodCntHalfB * 2 * (rodRadiusDiv * 2 + 1));
                }
                else
                {
                    System.Diagnostics.Debug.Assert(outofAreaRodPtCntRowTop == (rodRadiusDiv + 1));
                    System.Diagnostics.Debug.Assert(outofAreaRodPtCntRowBottom == (rodRadiusDiv + 1));
                    System.Diagnostics.Debug.Assert(rodVIIdsB1.Count == (rodCntHalfB * 2 * (rodRadiusDiv * 2 + 1) - outofAreaRodPtCntRowTop - outofAreaRodPtCntRowBottom));
                    System.Diagnostics.Debug.Assert(rodVIdsB2.Count == (rodCntHalfB * 2 * (rodRadiusDiv * 2 + 1) - outofAreaRodPtCntRowTop - outofAreaRodPtCntRowBottom));
                    System.Diagnostics.Debug.Assert(rodVIdsB3.Count == (rodCntHalfB * 2 * (rodRadiusDiv * 2 + 1) - outofAreaRodPtCntRowTop - outofAreaRodPtCntRowBottom));
                    System.Diagnostics.Debug.Assert(rodVIdsB4.Count == (rodCntHalfB * 2 * (rodRadiusDiv * 2 + 1) - outofAreaRodPtCntRowTop - outofAreaRodPtCntRowBottom));
                }

                /////////////////////////////////////////////////////////////////////////////
                // ロッドを追加
                uint F1NewPort1EId = 0;
                uint F2NewPort1EId = 0;
                uint F1DisconNewPort1EId = 0;
                uint F2DisconNewPport1EId = 0;
                uint F1NewPort2EId = 0;
                uint F2NewPort2EId = 0;
                uint F1DisconNewPort2EId = 0;
                uint F2DisconNewPort2EId = 0;
                uint B1TopRodCenterVId = 1;
                uint B1BottomRodCenterVId = 2;
                uint B2TopRodCenterVId = 8;
                uint B2BottomRodCenterVId = 3;
                uint B3TopRodCenterVId = 6;
                uint B3BottomRodCenterVId = 5;
                uint B4TopRodCenterVId = 7;
                uint B4BottomRodCenterVId = 4;

                // 左右のロッド(上下の強制境界と交差する円)と境界の交点
                IList<uint> rodQuarterVIdsF1 = new List<uint>();
                IList<uint> rodQuarterVIdsF2 = new List<uint>();

                for (int colIndex = 0; colIndex < 4; colIndex++) // このcolIndexは特に図面上のカラムを指すわけではない（ループ変数)
                {
                    // 上の強制境界と交差する点
                    if (isLargeRod
                        && ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0)))
                           )
                    {
                        uint[] eIds = new uint[2];
                        if (colIndex == 0)
                        {
                            // 入力境界 外側
                            // 入力導波路領域
                            eIds[0] = 8;
                            eIds[1] = 8;
                        }
                        else if (colIndex == 1)
                        {
                            // 入力境界内側
                            // 不連続領域
                            eIds[0] = 8;
                            eIds[1] = 7;
                        }
                        else if (colIndex == 2)
                        {
                            // 出力境界内側
                            // 不連続領域
                            eIds[0] = 7;
                            eIds[1] = 6;
                        }
                        else if (colIndex == 3)
                        {
                            // 出力境界外側
                            // 出力導波路領域
                            eIds[0] = 6;
                            eIds[1] = 6;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        double x0 = 0.0;
                        if (colIndex == 0 || colIndex == 1)
                        {
                            // 入力側
                            x0 = (rodDistanceX) * colIndex;
                        }
                        else if (colIndex == 2 || colIndex == 3)
                        {
                            // 出力側
                            x0 = inputWgLength + disconLength + (rodDistanceX) * (colIndex - 2);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        double y0 = WaveguideWidth;
                        double crossY = WaveguideWidth;
                        double[] crossXs = new double[2];
                        crossXs[0] = -1.0 * Math.Sqrt(rodRadius * rodRadius - (crossY - y0) * (crossY - y0)) + x0;
                        crossXs[1] = Math.Sqrt(rodRadius * rodRadius - (crossY - y0) * (crossY - y0)) + x0;
                        for (int k = 0; k < 2; k++)
                        {
                            uint eId = eIds[k];
                            double crossX = crossXs[k];
                            if (colIndex == 0 || colIndex == 1)
                            {
                                if (crossX <= (0.0 + Constants.PrecisionLowerLimit))
                                {
                                    continue;
                                }
                            }
                            else if (colIndex == 2 || colIndex == 3)
                            {
                                if (crossX >= (inputWgLength * 2.0 + disconLength - Constants.PrecisionLowerLimit))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                            var resAddVertex = cad2D.AddVertex(
                                CadElementType.Edge, eId, new OpenTK.Vector2d(crossX, crossY));
                            uint addVId = resAddVertex.AddVId;
                            uint addEId = resAddVertex.AddEId;
                            System.Diagnostics.Debug.Assert(addVId != 0);
                            System.Diagnostics.Debug.Assert(addEId != 0);
                            rodQuarterVIdsF1.Add(addVId);
                            eIdsF1.Add(addEId);
                            // 上側境界の中央部分の辺IDが新しくなる
                            if (colIndex == 0 && k == 1)
                            {
                                // 入力部
                                // 不変
                            }
                            else if (colIndex == 1 && k == 0)
                            {
                                // 不連続部
                                F1NewPort1EId = addEId;
                            }
                            else if (colIndex == 1 && k == 1)
                            {
                                // 不連続部
                                //不変
                            }
                            else if (colIndex == 2 && k == 0)
                            {
                                // 不連続部（出力側)
                                F1DisconNewPort1EId = addEId;
                                F1DisconNewPort2EId = addEId;
                            }
                            else if (colIndex == 2 && k == 1)
                            {
                                // 不連続部（出力側)
                                // 不変
                            }
                            else if (colIndex == 3 && k == 0)
                            {
                                // 出力部
                                F1NewPort2EId = addEId;
                            }
                            // DEBUG
                            //cad2d.SetColor_Edge(id_e_add, new double[] { 1.0, 0.0, 0.0 });
                        }
                    }
                }

                for (int colIndex = 3; colIndex >= 0; colIndex--) // このcolIndexは特に図面上のカラムを指すわけではない（ループ変数)
                {
                    // 下の強制境界と交差するロッド
                    if (isLargeRod
                        && ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0)))
                           )
                    {
                        uint[] eIds = new uint[2];
                        if (colIndex == 0)
                        {
                            // 入力境界 外側
                            // 入力導波路領域
                            eIds[0] = 2;
                            eIds[1] = 2;
                        }
                        else if (colIndex == 1)
                        {
                            // 入力境界内側
                            // 不連続領域
                            eIds[0] = 3;
                            eIds[1] = 2;
                        }
                        else if (colIndex == 2)
                        {
                            // 出力境界内側
                            // 不連続領域
                            eIds[0] = 4;
                            eIds[1] = 3;
                        }
                        else if (colIndex == 3)
                        {
                            // 入力境界 外側
                            // 入力導波路領域
                            eIds[0] = 4;
                            eIds[1] = 4;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        double x0 = 0.0;
                        if (colIndex == 0 || colIndex == 1)
                        {
                            // 入力側
                            x0 = (rodDistanceX) * colIndex;
                        }
                        else if (colIndex == 2 || colIndex == 3)
                        {
                            // 出力側
                            x0 = inputWgLength + disconLength + (rodDistanceX) * (colIndex - 2);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        double y0 = 0.0;
                        double crossY = 0.0;
                        double[] crossXs = new double[2];
                        crossXs[0] = Math.Sqrt(rodRadius * rodRadius - (crossY - y0) * (crossY - y0)) + x0;
                        crossXs[1] = -1.0 * Math.Sqrt(rodRadius * rodRadius - (crossY - y0) * (crossY - y0)) + x0;
                        for (int k = 0; k < 2; k++)
                        {
                            uint eId = eIds[k];
                            double crossX = crossXs[k];
                            if (colIndex == 0 || colIndex == 1)
                            {
                                if (crossX <= (0.0 + Constants.PrecisionLowerLimit))
                                {
                                    continue;
                                }
                            }
                            else if (colIndex == 2 || colIndex == 3)
                            {
                                if (crossX >= (inputWgLength * 2.0 + disconLength - Constants.PrecisionLowerLimit))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(false);
                            }
                            var resAddVertex = cad2D.AddVertex(
                                CadElementType.Edge, eId, new OpenTK.Vector2d(crossX, crossY));
                            uint addVId = resAddVertex.AddVId;
                            uint addEId = resAddVertex.AddEId;
                            System.Diagnostics.Debug.Assert(addVId != 0);
                            System.Diagnostics.Debug.Assert(addEId != 0);
                            rodQuarterVIdsF2.Add(addVId);
                            eIdsF2.Add(addEId);
                            // 下側境界の中央部分の辺IDが新しくなる
                            //   Note:colInde == 3から追加されることに注意
                            if (colIndex == 0 && k == 0)
                            {
                                // 入力部
                                F2NewPort1EId = addEId;
                            }
                            else if (colIndex == 0 && k == 1)
                            {
                                // 入力部
                                // 不変
                            }
                            else if (colIndex == 1 && k == 0)
                            {
                                // 不連続部
                                F2DisconNewPport1EId = addEId;
                                F2DisconNewPort2EId = addEId;
                            }
                            else if (colIndex == 2 && k == 1)
                            {
                                // 不連続部（出力側)
                                // 不変
                            }
                            else if (colIndex == 2 && k == 0)
                            {
                                // 不連続部（出力側)
                                F2NewPort2EId = addEId;
                            }
                            else if (colIndex == 3 && k == 1)
                            {
                                // 出力部
                                // 不変
                            }
                            // DEBUG
                            //cad2d.SetColor_Edge(id_e_add, new double[] { 1.0, 0.0, 0.0 });
                        }
                    }
                }

                // 左のロッドを追加
                for (int colIndex = 0; colIndex < 3; colIndex++) // このcolIndexは特に図面上のカラムを指すわけではない（ループ変数)
                {
                    // 左のロッド
                    IList<uint> workrodVIdsB = null;
                    double xB = 0;
                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    uint workBTopRodCenterVId = 0;
                    uint workBBottomRodCenterVId = 0;

                    // 始点、終点が逆？
                    bool isReverse = false;
                    if (colIndex == 0)
                    {
                        // 入力境界 外側
                        xB = 0.0;
                        workrodVIdsB = rodVIIdsB1;
                        // 入力導波路領域
                        baseLoopId = 1;
                        inputWgNo = 1;
                        workBTopRodCenterVId = B1TopRodCenterVId;
                        workBBottomRodCenterVId = B1BottomRodCenterVId;
                        isReverse = false;
                    }
                    else if (colIndex == 1)
                    {
                        // 入力境界 内側
                        xB = inputWgLength;
                        workrodVIdsB = rodVIdsB2;
                        // 不連続領域
                        baseLoopId = 2;
                        inputWgNo = 0;
                        workBTopRodCenterVId = B2TopRodCenterVId;
                        workBBottomRodCenterVId = B2BottomRodCenterVId;
                        isReverse = true;
                    }
                    else if (colIndex == 2)
                    {
                        // 出力境界 内側
                        xB = inputWgLength + disconLength;
                        workrodVIdsB = rodVIdsB4;
                        // 出力導波路領域
                        baseLoopId = 3;
                        inputWgNo = 2;
                        workBTopRodCenterVId = B4TopRodCenterVId;
                        workBBottomRodCenterVId = B4BottomRodCenterVId;
                        isReverse = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = rodCntHalfB - 1 - (int)((rodCntHalf - 1 - i) / 2);
                            int leftOfsIndex = 0;
                            if (isLargeRod && ((isShift180 && (rodCntHalf % 2 == 1)) ||
                                (!isShift180 && (rodCntHalf % 2 == 0))))
                            {
                                leftOfsIndex = -outofAreaRodPtCntRowTop;
                            }
                            bool isQuarterRod = false;
                            // 左のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                if (workrodVIdsB == rodVIIdsB1)
                                {
                                    int indexV0 =
                                        (workrodVIdsB.Count - (rodRadiusDiv * 2 + 1) -
                                        i2 * (rodRadiusDiv * 2 + 1)) - leftOfsIndex;
                                    int indexV1 =
                                        (workrodVIdsB.Count - (rodRadiusDiv + 1) -
                                        i2 * (rodRadiusDiv * 2 + 1)) - leftOfsIndex;
                                    int indexV2 = (workrodVIdsB.Count - 1 -
                                        i2 * (rodRadiusDiv * 2 + 1)) - leftOfsIndex;
                                    if (indexV2 > workrodVIdsB.Count - 1)
                                    {
                                        isQuarterRod = true;
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workBTopRodCenterVId;
                                        vId2 = rodQuarterVIdsF1[0 + colIndex * 2]; // 1つ飛ばしで参照;
                                    }
                                    else
                                    {
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workrodVIdsB[indexV1];
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                }
                                else
                                {
                                    int indexV0 = (0 + i2 * (rodRadiusDiv * 2 + 1)) + leftOfsIndex;
                                    int indexV1 = ((rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)) + leftOfsIndex;
                                    int indexV2 = ((rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)) + leftOfsIndex;
                                    if (indexV0 < 0)
                                    {
                                        isQuarterRod = true;
                                        vId0 = rodQuarterVIdsF1[0 + colIndex * 2]; // 1つ飛ばしで参照
                                        vId1 = workBTopRodCenterVId;
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                    else
                                    {
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workrodVIdsB[indexV1];
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                }
                                double x0 = xB;
                                double y0 = WaveguideWidth - i * rodDistanceY - rodDistanceY * 0.5;
                                if (isLargeRod)
                                {
                                    y0 += rodDistanceY * 0.5;
                                }
                                uint workVId0 = vId0;
                                uint workVId2 = vId2;
                                if (isReverse)
                                {
                                    workVId0 = vId2;
                                    workVId2 = vId0;
                                }
                                uint lId = 0;
                                if (isQuarterRod)
                                {
                                    // 1/4円を追加する
                                    lId = PCWaveguideUtils.AddExactlyQuarterRod(
                                        cad2D,
                                        baseLoopId,
                                        x0,
                                        y0,
                                        rodRadius,
                                        rodCircleDiv,
                                        rodRadiusDiv,
                                        workVId2,
                                        vId1,
                                        workVId0,
                                        0.0,
                                        true);
                                }
                                else
                                {
                                    // 左のロッド
                                    lId = PCWaveguideUtils.AddLeftRod(
                                        cad2D,
                                        baseLoopId,
                                        workVId0,
                                        vId1,
                                        workVId2,
                                        x0,
                                        y0,
                                        rodRadius,
                                        rodCircleDiv,
                                        rodRadiusDiv);
                                }
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }

                            }
                        }
                    }
                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if (i % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = i / 2;
                            int ofs_index_left = 0;
                            if (isLargeRod &&
                                ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0))))
                            {
                                ofs_index_left = -outofAreaRodPtCntRowTop;
                            }
                            bool isQuarterRod = false;
                            // 左のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                if (workrodVIdsB == rodVIIdsB1)
                                {
                                    int indexV0 =
                                        (workrodVIdsB.Count / 2 - (rodRadiusDiv * 2 + 1) - i2 * (rodRadiusDiv * 2 + 1));
                                    int indexV1 =
                                        (workrodVIdsB.Count / 2 - (rodRadiusDiv + 1) - i2 * (rodRadiusDiv * 2 + 1));
                                    int indexV2 =
                                        (workrodVIdsB.Count / 2 - 1 - i2 * (rodRadiusDiv * 2 + 1));
                                    if (indexV0 < 0)
                                    {
                                        isQuarterRod = true;
                                        vId0 = rodQuarterVIdsF2[rodQuarterVIdsF2.Count - 1 - colIndex * 2]; // 1つ飛ばしで参照
                                        vId1 = workBBottomRodCenterVId;
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                    else
                                    {
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workrodVIdsB[indexV1];
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                }
                                else
                                {
                                    int indexV0 = (workrodVIdsB.Count / 2 + 0 + i2 * (rodRadiusDiv * 2 + 1));
                                    int indexV1 = (workrodVIdsB.Count / 2 + (rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1));
                                    int indexV2 =
                                        (workrodVIdsB.Count / 2 + (rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1));
                                    if (indexV2 > workrodVIdsB.Count - 1)
                                    {
                                        isQuarterRod = true;
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workBBottomRodCenterVId;
                                        vId2 = rodQuarterVIdsF2[rodQuarterVIdsF2.Count - 1 - colIndex * 2]; // 1つ飛ばしで参照
                                    }
                                    else
                                    {
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workrodVIdsB[indexV1];
                                        vId2 = workrodVIdsB[indexV2];
                                    }

                                }
                                double x0 = xB;
                                double y0 = rodDistanceY * rodCntHalf - i * rodDistanceY - rodDistanceY * 0.5;
                                if (isLargeRod)
                                {
                                    y0 -= rodDistanceY * 0.5;
                                }
                                uint work_id_v0 = vId0;
                                uint work_id_v2 = vId2;
                                if (isReverse)
                                {
                                    work_id_v0 = vId2;
                                    work_id_v2 = vId0;
                                }
                                uint lId = 0;
                                if (isQuarterRod)
                                {
                                    // 1/4円を追加する
                                    lId = PCWaveguideUtils.AddExactlyQuarterRod(
                                        cad2D,
                                        baseLoopId,
                                        x0,
                                        y0,
                                        rodRadius,
                                        rodCircleDiv,
                                        rodRadiusDiv,
                                        work_id_v2,
                                        vId1,
                                        work_id_v0,
                                        90.0,
                                        true);
                                }
                                else
                                {
                                    // 左のロッド
                                    lId = PCWaveguideUtils.AddLeftRod(
                                         cad2D,
                                         baseLoopId,
                                         work_id_v0,
                                         vId1,
                                         work_id_v2,
                                         x0,
                                         y0,
                                         rodRadius,
                                         rodCircleDiv,
                                         rodRadiusDiv);
                                }
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                }

                // 右のロッドを追加
                for (int colIndex = 0; colIndex < 3; colIndex++) // このcolIndexは特に図面上のカラムを指すわけではない（ループ変数)
                {
                    // 右のロッド
                    IList<uint> workrodVIdsB = null;
                    double xB = 0;
                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    uint workBTopRodCenterVId = 0;
                    uint workBBottomRodCenterVId = 0;

                    if (colIndex == 0)
                    {
                        // 入力境界 内側
                        xB = inputWgLength;
                        workrodVIdsB = rodVIdsB2;
                        // 入力導波路領域
                        baseLoopId = 1;
                        inputWgNo = 1;
                        workBTopRodCenterVId = B2TopRodCenterVId;
                        workBBottomRodCenterVId = B2BottomRodCenterVId;
                    }
                    else if (colIndex == 1)
                    {
                        // 出力境界 内側
                        xB = inputWgLength + disconLength;
                        workrodVIdsB = rodVIdsB4;
                        // 不連続領域
                        baseLoopId = 2;
                        inputWgNo = 0;
                        workBTopRodCenterVId = B4TopRodCenterVId;
                        workBBottomRodCenterVId = B4BottomRodCenterVId;
                    }
                    else if (colIndex == 2)
                    {
                        // 出力境界 外側
                        xB = inputWgLength * 2.0 + disconLength;
                        workrodVIdsB = rodVIdsB3;
                        // 出力導波路領域
                        baseLoopId = 3;
                        inputWgNo = 2;
                        workBTopRodCenterVId = B3TopRodCenterVId;
                        workBBottomRodCenterVId = B3BottomRodCenterVId;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = rodCntHalfB - 1 - (int)((rodCntHalf - 1 - i) / 2);
                            int topOfsIndex = 0;
                            if (isLargeRod &&
                                ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0))))
                            {
                                topOfsIndex = -outofAreaRodPtCntRowTop;
                            }
                            bool isQuarterRod = false;

                            // 右のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                if (workrodVIdsB == rodVIIdsB1)
                                {
                                    int index_v0 = (workrodVIdsB.Count - (rodRadiusDiv * 2 + 1) - i2 * (rodRadiusDiv * 2 + 1)) - topOfsIndex;
                                    int index_v1 = (workrodVIdsB.Count - (rodRadiusDiv + 1) - i2 * (rodRadiusDiv * 2 + 1)) - topOfsIndex;
                                    int index_v2 = (workrodVIdsB.Count - 1 - i2 * (rodRadiusDiv * 2 + 1));
                                    if (index_v2 > workrodVIdsB.Count - 1)
                                    {
                                        isQuarterRod = true;
                                        vId0 = workrodVIdsB[index_v0];
                                        vId1 = B2TopRodCenterVId;
                                        vId2 = rodQuarterVIdsF1[1 + colIndex * 2]; // 1つ飛ばしで参照;
                                    }
                                    else
                                    {
                                        vId0 = workrodVIdsB[index_v0];
                                        vId1 = workrodVIdsB[index_v1];
                                        vId2 = workrodVIdsB[index_v2];
                                    }
                                }
                                else
                                {
                                    int indexV0 = (0 + i2 * (rodRadiusDiv * 2 + 1)) + topOfsIndex;
                                    int indexV1 = ((rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1)) + topOfsIndex;
                                    int indexV2 = ((rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1)) + topOfsIndex;
                                    if (indexV0 < 0)
                                    {
                                        isQuarterRod = true;
                                        vId0 = rodQuarterVIdsF1[1 + colIndex * 2];
                                        vId1 = workBTopRodCenterVId;
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                    else
                                    {
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workrodVIdsB[indexV1];
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                }
                                double x0 = xB;
                                double y0 = WaveguideWidth - i * rodDistanceY - rodDistanceY * 0.5;
                                if (isLargeRod)
                                {
                                    y0 += rodDistanceY * 0.5;
                                }
                                OpenTK.Vector2d centerPt = cad2D.GetVertexCoord(vId1);
                                uint lId = 0;
                                if (isQuarterRod)
                                {
                                    // 1/4円を追加する
                                    lId = PCWaveguideUtils.AddExactlyQuarterRod(
                                        cad2D,
                                        baseLoopId,
                                        x0,
                                        y0,
                                        rodRadius,
                                        rodCircleDiv,
                                        rodRadiusDiv,
                                        vId2,
                                        vId1,
                                        vId0,
                                        270.0,
                                        true);
                                }
                                else
                                {
                                    // 右のロッド
                                    lId = PCWaveguideUtils.AddRightRod(
                                        cad2D,
                                        baseLoopId,
                                        vId0,
                                        vId1,
                                        vId2,
                                        x0,
                                        y0,
                                        rodRadius,
                                        rodCircleDiv,
                                        rodRadiusDiv);
                                }
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if (i % 2 == (isShift180 ? 0 : 1))
                        {
                            int i2 = i / 2;
                            int topOfsIndex = 0;
                            if (isLargeRod
                                && ((isShift180 && (rodCntHalf % 2 == 1)) || (!isShift180 && (rodCntHalf % 2 == 0))))
                            {
                                topOfsIndex = -outofAreaRodPtCntRowTop;
                            }
                            bool isQuarterRod = false;
                            // 右のロッド
                            {
                                uint vId0 = 0;
                                uint vId1 = 0;
                                uint vId2 = 0;
                                if (workrodVIdsB == rodVIIdsB1)
                                {
                                    int indexV0 =
                                        (workrodVIdsB.Count / 2 - (rodRadiusDiv * 2 + 1) - i2 * (rodRadiusDiv * 2 + 1));
                                    int indexV1 =
                                        (workrodVIdsB.Count / 2 - (rodRadiusDiv + 1) - i2 * (rodRadiusDiv * 2 + 1));
                                    int indexV2 =
                                        (workrodVIdsB.Count / 2 - 1 - i2 * (rodRadiusDiv * 2 + 1));
                                    if (indexV0 < 0)
                                    {
                                        isQuarterRod = true;
                                        vId0 = rodQuarterVIdsF2[rodQuarterVIdsF2.Count - 2 - colIndex * 2]; // 1つ飛ばしで参照
                                        vId1 = workBBottomRodCenterVId;
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                    else
                                    {
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workrodVIdsB[indexV1];
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                }
                                else
                                {
                                    int indexV0 = (workrodVIdsB.Count / 2 + 0 + i2 * (rodRadiusDiv * 2 + 1));
                                    int indexV1 =
                                        (workrodVIdsB.Count / 2 + (rodRadiusDiv) + i2 * (rodRadiusDiv * 2 + 1));
                                    int indexV2 =
                                        (workrodVIdsB.Count / 2 + (rodRadiusDiv * 2) + i2 * (rodRadiusDiv * 2 + 1));
                                    if (indexV2 > workrodVIdsB.Count - 1)
                                    {
                                        isQuarterRod = true;
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workBBottomRodCenterVId;
                                        vId2 = rodQuarterVIdsF2[rodQuarterVIdsF2.Count - 2 - colIndex * 2]; // 1つ飛ばしで参照
                                    }
                                    else
                                    {
                                        vId0 = workrodVIdsB[indexV0];
                                        vId1 = workrodVIdsB[indexV1];
                                        vId2 = workrodVIdsB[indexV2];
                                    }
                                }
                                double x0 = xB;
                                double y0 = rodDistanceY * rodCntHalf - i * rodDistanceY - rodDistanceY * 0.5;
                                if (isLargeRod)
                                {
                                    y0 -= rodDistanceY * 0.5;
                                }
                                uint lId = 0;
                                if (isQuarterRod)
                                {
                                    // 1/4円を追加する
                                    lId = PCWaveguideUtils.AddExactlyQuarterRod(
                                        cad2D,
                                        baseLoopId,
                                        x0,
                                        y0,
                                        rodRadius,
                                        rodCircleDiv,
                                        rodRadiusDiv,
                                        vId2,
                                        vId1,
                                        vId0,
                                        180.0,
                                        true);
                                }
                                else
                                {
                                    // 右のロッド
                                    lId = PCWaveguideUtils.AddRightRod(
                                      cad2D,
                                      baseLoopId,
                                      vId0,
                                      vId1,
                                      vId2,
                                      x0,
                                      y0,
                                      rodRadius,
                                      rodCircleDiv,
                                      rodRadiusDiv);
                                }
                                rodLoopIds.Add(lId);
                                if (inputWgNo == 1)
                                {
                                    inputWgRodLoopIds1.Add(lId);
                                }
                                else if (inputWgNo == 2)
                                {
                                    inputWgRodLoopIds2.Add(lId);
                                }
                            }
                        }
                    }
                }

                // 中央ロッド
                int periodCntInputWg1 = 1;
                int periodCntInputWg2 = 1;
                int periodCntX = periodCntInputWg1 + rodCntDiscon + periodCntInputWg2;

                // 中央のロッド(上下の強制境界と交差する円)と境界の交点
                IList<uint> vIdsF1 = new List<uint>();
                IList<uint> vIdsF2 = new List<uint>();
                for (int col = 1; col <= (periodCntX * 2 - 1); col++)
                {
                    if (col == (periodCntInputWg1 * 2)) continue; // 入力導波路内部境界 (既にロッド追加済み)
                    if (col == (periodCntInputWg1 + rodCntDiscon) * 2) continue; // 出力導波路内部境界  (既にロッド追加済み)
                    if (col == (periodCntX * 2)) continue; // 出力導波路外側境界  (既にロッド追加済み)
                    int inputWgNo = 0;
                    if (col >= 0 && col < (periodCntInputWg1 * 2))
                    {
                        inputWgNo = 1;
                    }
                    else if (col >= (periodCntInputWg1 * 2 + 1) && col < (periodCntInputWg1 + rodCntDiscon) * 2)
                    {
                        inputWgNo = 0;
                    }
                    else if (col >= ((periodCntInputWg1 + rodCntDiscon) * 2 + 1) && col < ((periodCntInputWg1 + rodCntDiscon + periodCntInputWg2) * 2))
                    {
                        inputWgNo = 2;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    // 上の強制境界と交差するロッド
                    if (isLargeRod
                           && ((col % 2 == 1 && ((rodCntHalf - 1 - 0) % 2 == (isShift180 ? 1 : 0)))
                               || (col % 2 == 0 && ((rodCntHalf - 1 - 0) % 2 == (isShift180 ? 0 : 1)))
                                )
                        )
                    {
                        uint eId = 0;
                        if (inputWgNo == 1)
                        {
                            eId = 8;
                            //if (!isShift180)
                            if (F1NewPort1EId != 0)
                            {
                                eId = F1NewPort1EId;
                            }
                        }
                        else if (inputWgNo == 0)
                        {
                            eId = 7;
                            //if (!isShift180)
                            if (F1DisconNewPort1EId != 0)
                            {
                                eId = F1DisconNewPort1EId;
                            }
                        }
                        else if (inputWgNo == 2)
                        {
                            eId = 6;
                            //if (!isShift180)
                            if (F1NewPort2EId != 0)
                            {
                                eId = F1NewPort2EId;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        double x0 = rodDistanceX * 0.5 * col;
                        double y0 = WaveguideWidth;
                        double crossY = WaveguideWidth;
                        double[] crossXs = new double[3];
                        crossXs[0] = -1.0 * Math.Sqrt(rodRadius * rodRadius - (crossY - y0) * (crossY - y0)) + x0; // 交点
                        crossXs[1] = x0; // 中心
                        crossXs[2] = Math.Sqrt(rodRadius * rodRadius - (crossY - y0) * (crossY - y0)) + x0; // 交点
                        foreach (double x_cross in crossXs)
                        {
                            var resAddVertex =
                                cad2D.AddVertex(CadElementType.Edge, eId, new OpenTK.Vector2d(x_cross, crossY));
                            uint addVId = resAddVertex.AddVId;
                            uint addEId = resAddVertex.AddEId;
                            System.Diagnostics.Debug.Assert(addVId != 0);
                            System.Diagnostics.Debug.Assert(addEId != 0);
                            vIdsF1.Add(addVId);
                            eIdsF1.Add(addEId);
                        }
                    }
                }

                for (int col = (periodCntX * 2 - 1); col >= 1; col--)
                {
                    if (col == (periodCntInputWg1 * 2)) continue; // 入力導波路内部境界 (既にロッド追加済み)
                    if (col == (periodCntInputWg1 + rodCntDiscon) * 2) continue; // 出力導波路内部境界  (既にロッド追加済み)
                    if (col == (periodCntX * 2)) continue; // 出力導波路外側境界  (既にロッド追加済み)
                    int inputWgNo = 0;
                    if (col >= 0 && col < (periodCntInputWg1 * 2))
                    {
                        inputWgNo = 1;
                    }
                    else if (col >= (periodCntInputWg1 * 2 + 1) && col < (periodCntInputWg1 + rodCntDiscon) * 2)
                    {
                        inputWgNo = 0;
                    }
                    else if (col >= ((periodCntInputWg1 + rodCntDiscon) * 2 + 1) &&
                        col < ((periodCntInputWg1 + rodCntDiscon + periodCntInputWg2) * 2))
                    {
                        inputWgNo = 2;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    // 下の強制境界と交差するロッド
                    if (isLargeRod
                           && ((col % 2 == 1 && ((rodCntHalf - 1 - 0) % 2 == (isShift180 ? 1 : 0)))
                              || (col % 2 == 0 && ((rodCntHalf - 1 - 0) % 2 == (isShift180 ? 0 : 1)))
                              )
                        )
                    {
                        uint eId = 0;
                        if (inputWgNo == 1)
                        {
                            eId = 2;
                            //if (!isShift180)
                            if (F2NewPort1EId != 0)
                            {
                                eId = F2NewPort1EId;
                            }
                        }
                        else if (inputWgNo == 0)
                        {
                            eId = 3;
                            //if (!isShift180)
                            if (F2DisconNewPport1EId != 0)
                            {
                                eId = F2DisconNewPport1EId;
                            }
                        }
                        else if (inputWgNo == 2)
                        {
                            eId = 4;
                            //if (!isShift180)
                            if (F2NewPort2EId != 0)
                            {
                                eId = F2NewPort2EId;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                        double x0 = rodDistanceX * 0.5 * col;
                        double y0 = 0.0;
                        double crossY = 0.0;
                        double[] crossXs = new double[3];
                        crossXs[0] = Math.Sqrt(rodRadius * rodRadius - (crossY - y0) * (crossY - y0)) + x0; // 交点
                        crossXs[1] = x0; // 中心
                        crossXs[2] = -1.0 * Math.Sqrt(rodRadius * rodRadius - (crossY - y0) * (crossY - y0)) + x0; // 交点
                        foreach (double x_cross in crossXs)
                        {
                            var resAddVertex =
                                cad2D.AddVertex(CadElementType.Edge, eId, new OpenTK.Vector2d(x_cross, crossY));
                            uint addVId = resAddVertex.AddVId;
                            uint addEId = resAddVertex.AddEId;
                            System.Diagnostics.Debug.Assert(addVId != 0);
                            System.Diagnostics.Debug.Assert(addEId != 0);
                            vIdsF2.Add(addVId);
                            eIdsF2.Add(addEId);
                        }
                    }
                }

                // 中央のロッド
                for (int col = 1; col <= (periodCntX * 2 - 1); col++)
                {
                    if (col == (periodCntInputWg1 * 2)) continue; // 入力導波路内部境界 (既にロッド追加済み)
                    if (col == (periodCntInputWg1 + rodCntDiscon) * 2) continue; // 出力導波路内部境界  (既にロッド追加済み)
                    if (col == (periodCntX * 2)) continue; // 出力導波路外側境界  (既にロッド追加済み)
                    uint baseLoopId = 0;
                    int inputWgNo = 0;
                    if (col >= 0 && col < (periodCntInputWg1 * 2))
                    {
                        baseLoopId = 1;
                        inputWgNo = 1;
                    }
                    else if (col >= (periodCntInputWg1 * 2 + 1) && col < (periodCntInputWg1 + rodCntDiscon) * 2)
                    {
                        baseLoopId = 2;
                        inputWgNo = 0;
                    }
                    else if (col >= ((periodCntInputWg1 + rodCntDiscon) * 2 + 1) &&
                        col < ((periodCntInputWg1 + rodCntDiscon + periodCntInputWg2) * 2))
                    {
                        baseLoopId = 3;
                        inputWgNo = 2;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    // 中央のロッド
                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if (isLargeRod &&
                              ((col % 2 == 1 && ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 1 : 0)))
                                || (col % 2 == 0 && ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1))))
                            )
                        {
                            if (i == 0)
                            {
                                {
                                    // 半円（下半分)を追加
                                    double x0 = rodDistanceX * 0.5 * col;
                                    double y0 = WaveguideWidth - i * rodDistanceY - rodDistanceY * 0.5;
                                    if (isLargeRod)
                                    {
                                        y0 += rodDistanceY * 0.5; // for isLargeRod
                                    }
                                    int col2 = col / 2;
                                    if (isShift180 &&
                                        (col % 2 == 0 && ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1))))
                                    {
                                        col2 = col2 - 2;
                                    }
                                    else if (!isShift180 &&
                                        (col % 2 == 0 && ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1))))
                                    {
                                        col2 = col2 - 2;
                                    }
                                    uint vId0 = vIdsF1[col2 * 3 + 0];
                                    uint vId1 = vIdsF1[col2 * 3 + 1];
                                    uint vId2 = vIdsF1[col2 * 3 + 2];
                                    uint lId = PCWaveguideUtils.AddExactlyHalfRod(
                                        cad2D,
                                        baseLoopId,
                                        vId0,
                                        vId1,
                                        vId2,
                                        x0,
                                        y0,
                                        rodRadius,
                                        rodCircleDiv,
                                        rodRadiusDiv,
                                        0.0,
                                        true);
                                    rodLoopIds.Add(lId);
                                    if (inputWgNo == 1)
                                    {
                                        inputWgRodLoopIds1.Add(lId);
                                    }
                                    else if (inputWgNo == 2)
                                    {
                                        inputWgRodLoopIds2.Add(lId);
                                    }
                                }

                                continue;
                            }
                        }
                        if ((col % 2 == 1 && ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 1 : 0)))
                            || (col % 2 == 0 && ((rodCntHalf - 1 - i) % 2 == (isShift180 ? 0 : 1))))
                        {
                            // 中央ロッド
                            double x0 = rodDistanceX * 0.5 * col;
                            double y0 = WaveguideWidth - i * rodDistanceY - rodDistanceY * 0.5;
                            if (isLargeRod)
                            {
                                y0 += rodDistanceY * 0.5; // for isLargeRod
                            }
                            uint lId = PCWaveguideUtils.AddRod(
                                cad2D, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                            rodLoopIds.Add(lId);
                            if (inputWgNo == 1)
                            {
                                inputWgRodLoopIds1.Add(lId);
                            }
                            else if (inputWgNo == 2)
                            {
                                inputWgRodLoopIds2.Add(lId);
                            }
                        }
                    }
                    for (int i = 0; i < rodCntHalf; i++)
                    {
                        if (isLargeRod
                               && ((col % 2 == 1 && (i % 2 == (isShift180 ? 1 : 0)))
                                    || (col % 2 == 0 && (i % 2 == (isShift180 ? 0 : 1))))
                               )
                        {
                            if (i == (rodCntHalf - 1))
                            {
                                {
                                    // 半円（上半分)を追加
                                    double x0 = rodDistanceX * 0.5 * col;
                                    double y0 = rodDistanceY * rodCntHalf - i * rodDistanceY - rodDistanceY * 0.5;
                                    if (isLargeRod)
                                    {
                                        y0 -= rodDistanceY * 0.5; // for isLargeRod
                                    }
                                    int col2 = (periodCntX * 2 - 1 - col) / 2;
                                    if (isShift180 && (col % 2 == 0 && (i % 2 == (isShift180 ? 0 : 1))))
                                    {
                                        col2 = col2 - 1;
                                    }
                                    else if (!isShift180 && (col % 2 == 0 && (i % 2 == (isShift180 ? 0 : 1))))
                                    {
                                        col2 = col2 - 1;
                                    }
                                    uint vId0 = vIdsF2[col2 * 3 + 0];
                                    uint vId1 = vIdsF2[col2 * 3 + 1];
                                    uint vId2 = vIdsF2[col2 * 3 + 2];
                                    uint lId = PCWaveguideUtils.AddExactlyHalfRod(
                                        cad2D,
                                        baseLoopId,
                                        vId0,
                                        vId1,
                                        vId2,
                                        x0,
                                        y0,
                                        rodRadius,
                                        rodCircleDiv,
                                        rodRadiusDiv,
                                        180.0,
                                        true);
                                    rodLoopIds.Add(lId);
                                    if (inputWgNo == 1)
                                    {
                                        inputWgRodLoopIds1.Add(lId);
                                    }
                                    else if (inputWgNo == 2)
                                    {
                                        inputWgRodLoopIds2.Add(lId);
                                    }
                                }

                                continue;
                            }
                        }
                        if ((col % 2 == 1 && (i % 2 == (isShift180 ? 1 : 0)))
                            || (col % 2 == 0 && (i % 2 == (isShift180 ? 0 : 1))))
                        {
                            // 中央ロッド
                            double x0 = rodDistanceX * 0.5 * col;
                            double y0 = rodDistanceY * rodCntHalf - i * rodDistanceY - rodDistanceY * 0.5;
                            if (isLargeRod)
                            {
                                y0 -= rodDistanceY * 0.5; // for isLargeRod
                            }
                            uint lId = PCWaveguideUtils.AddRod(
                                cad2D, baseLoopId, x0, y0, rodRadius, rodCircleDiv, rodRadiusDiv);
                            rodLoopIds.Add(lId);
                            if (inputWgNo == 1)
                            {
                                inputWgRodLoopIds1.Add(lId);
                            }
                            else if (inputWgNo == 2)
                            {
                                inputWgRodLoopIds2.Add(lId);
                            }
                        }
                    }
                }
            }

            mainWindow.IsFieldDraw = false;
            var drawerArray = mainWindow.DrawerArray;
            drawerArray.Clear();
            IDrawer drawer = new CadObject2DDrawer(cad2D);
            mainWindow.DrawerArray.Add(drawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
            WPFUtils.DoEvents();

            Mesher2D mesher2D = new Mesher2D(cad2D, eLen);

            /*
            mainWindow.IsFieldDraw = false;
            drawerArray.Clear();
            IDrawer meshDrawer = new Mesher2DDrawer(mesher2D);
            mainWindow.DrawerArray.Add(meshDrawer);
            mainWindow.Camera.Fit(drawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
            mainWindow.glControl_ResizeProc();
            mainWindow.glControl.Invalidate();
            mainWindow.glControl.Update();
            WPFUtils.DoEvents();
            */

            FEWorld world = new FEWorld();
            world.Mesh = mesher2D;
            uint quantityId;
            {
                uint dof = 1; // 複素数
                uint feOrder = 1;
                quantityId = world.AddQuantity(dof, feOrder, FiniteElementType.ScalarLagrange);
            }

            uint claddingMaId = uint.MaxValue;
            uint coreMaId = uint.MaxValue;
            {
                world.ClearMaterial();

                claddingMaId = world.AddMaterial(claddingMa);
                coreMaId = world.AddMaterial(coreMa);

                uint[] lIds = new uint[3 + rodLoopIds.Count];
                lIds[0] = 1;
                lIds[1] = 2;
                lIds[2] = 3;
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    lIds[i + 3] = rodLoopIds[i];
                }
                uint[] maIds = new uint[3 + rodLoopIds.Count];
                for (int i = 0; i < 3; i++)
                {
                    maIds[i] = claddingMaId;
                }
                for (int i = 0; i < rodLoopIds.Count; i++)
                {
                    maIds[i + 3] = coreMaId;
                }

                for (int i = 0; i < lIds.Length; i++)
                {
                    uint lId = lIds[i];
                    uint maId = maIds[i];
                    world.SetCadLoopMaterial(lId, maId);
                }
            }

            IList<PCWaveguidePortInfo> wgPortInfos = new List<PCWaveguidePortInfo>();
            // ２ポート情報リスト作成
            const uint portCnt = 2;
            bool[] isPortBc2Reverse = { true, false };
            for (int portId = 0; portId < portCnt; portId++)
            {
                var wgPortInfo = new PCWaveguidePortInfo();
                wgPortInfos.Add(wgPortInfo);
                System.Diagnostics.Debug.Assert(wgPortInfos.Count == (portId + 1));
                //wgPortInfo.IsSVEA = true; // 緩慢変化包絡線近似
                wgPortInfo.IsSVEA = false; // Φを直接解く
                wgPortInfo.IsPortBc2Reverse = isPortBc2Reverse[portId];
                wgPortInfo.LatticeA = latticeA;
                wgPortInfo.PeriodicDistance = periodicDistance;
                wgPortInfo.MinEffN = minEffN;
                wgPortInfo.MaxEffN = maxEffN;
                wgPortInfo.MinWaveNum = minWaveNum;
                wgPortInfo.MaxWaveNum = maxWaveNum;
                wgPortInfo.ReplacedMu0 = replacedMu0;
            }

            // 開口条件
            // 周期構造境界1
            for (int portIndex = 0; portIndex < portCnt; portIndex++)
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                IList<uint> workrodEIdsB = null;

                if (portIndex == 0)
                {
                    eIds[0] = 1;
                    workrodEIdsB = rodEIdsB1;
                }
                else if (portIndex == 1)
                {
                    eIds[0] = 5;
                    workrodEIdsB = rodEIdsB3;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                if (workrodEIdsB.Contains(eIds[0]))
                {
                    maIds[0] = coreMaId;
                }
                else
                {
                    maIds[0] = claddingMaId;
                }

                for (int i = 1; i <= divCnt - 1; i++)
                {
                    if (portIndex == 0)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) - (i - 1));
                    }
                    else if (portIndex == 1)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) * 3 - (i - 1));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    if (workrodEIdsB.Contains(eIds[i]))
                    {
                        maIds[i] = coreMaId;
                    }
                    else
                    {
                        maIds[i] = claddingMaId;
                    }
                }

                for (int i = 0; i < eIds.Length; i++)
                {
                    uint eId = eIds[i];
                    uint maId = maIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }

                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portIndex];
                wgPortInfo.BcEdgeIds1 = new List<uint>(eIds);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            // 周期構造入出力導波路
            for (int portId = 0; portId < portCnt; portId++)
            {
                // ワールド座標系のループIDを取得
                uint[] lIds = null;
                if (portId == 0)
                {
                    
                    lIds = new uint[1 + inputWgRodLoopIds1.Count];
                    lIds[0] = 1;
                    for (int i = 0; i < inputWgRodLoopIds1.Count; i++)
                    {
                        lIds[i + 1] = inputWgRodLoopIds1[i];
                    }
                }
                else if (portId == 1)
                {
                    lIds = new uint[1 + inputWgRodLoopIds2.Count];
                    lIds[0] = 3;
                    for (int i = 0; i < inputWgRodLoopIds2.Count; i++)
                    {
                        lIds[i + 1] = inputWgRodLoopIds2[i];
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.LoopIds = new List<uint>(lIds);
            }
            // 周期構造境界2
            for (int portId = 0; portId < portCnt; portId++)
            {
                uint[] eIds = new uint[divCnt];
                uint[] maIds = new uint[eIds.Length];
                IList<uint> workrodEIdsB = null;

                if (portId == 0)
                {
                    eIds[0] = 9;
                    workrodEIdsB = rodEIdsB2;
                }
                else if (portId == 1)
                {
                    eIds[0] = 10;
                    workrodEIdsB = rodEIdsB4;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                if (workrodEIdsB.Contains(eIds[0]))
                {
                    maIds[0] = coreMaId;
                }
                else
                {
                   maIds[0] = claddingMaId;
                }

                for (int i = 1; i <= divCnt - 1; i++)
                {
                    if (portId == 0)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) * 2 - (i - 1));
                    }
                    else if (portId == 1)
                    {
                        eIds[i] = (uint)(10 + (divCnt - 1) * 4 - (i - 1));
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    if (workrodEIdsB.Contains(eIds[i]))
                    {
                        maIds[i] = coreMaId;
                    }
                    else
                    {
                        maIds[i] = claddingMaId;
                    }
                }

                for (int i = 0; i < eIds.Length; i++)
                {
                    uint eId = eIds[i];
                    uint maId = maIds[i];
                    world.SetCadEdgeMaterial(eId, maId);
                }

                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.BcEdgeIds2 = new List<uint>(eIds);
            }

            // ポート条件
            IList<PortCondition> portConditions = world.GetPortConditions(quantityId);
            {
                portConditions.Clear();
                world.SetIncidentPortId(quantityId, 0);
                world.SetIncidentModeId(quantityId, 0);
                for (int portId = 0; portId < portCnt; portId++)
                {
                    PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                    IList<uint> lIds = wgPortInfo.LoopIds;
                    IList<uint> bcEIds1 = wgPortInfo.BcEdgeIds1;
                    IList<uint> bcEIds2 = wgPortInfo.BcEdgeIds2;
                    PortCondition portCondition = new PortCondition(
                        lIds, bcEIds1, bcEIds2, FieldValueType.ZScalar, new List<uint> { 0 }, 0);
                    portConditions.Add(portCondition);
                }
            }

            /*
            // 強制境界
            // TMモードの場合磁気壁
            uint[] zeroEIds = new uint[6 + eIdsF1.Count + eIdsF2.Count];
            Array.Copy(new uint[]{ 2, 3, 4, 6, 7, 8 }, zeroEIds, 0);
            for (int i = 0; i < eIdsF1.Count; i++)
            {
                zeroEIds[6 + i] = eIdsF1[i];
            }
            for (int i = 0; i < eIdsF2.Count; i++)
            {
                zeroEIds[6 + eIdsF1.Count + i] = eIdsF2[i];
            }
            var zeroFixedCads = world.GetZeroFieldFixedCads(quantityId);
            zeroFixedCads.Clear();
            foreach (uint eId in zeroEIds)
            {
                // 複素数
                var fixedCad = new FieldFixedCad(eId, CadElementType.Edge, FieldValueType.ZScalar);
                zeroFixedCads.Add(fixedCad);
            }
            */

            world.MakeElements();

            // ポートの境界上の節点を準備する
            for (int portId = 0; portId < portCnt; portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.SetupAfterMakeElements(world, quantityId, (uint)portId);
            }
            // フォトニック結晶導波路チャンネル上節点を取得する
            for (int portId = 0; portId < portCnt; portId++)
            {
                PCWaveguidePortInfo wgPortInfo = wgPortInfos[portId];
                wgPortInfo.PCChannelCoIds = new List<IList<int>>();

                // 全座標IDの取得
                IList<int> coIds = new List<int>();
                IList<uint> lIds = wgPortInfo.LoopIds;
                foreach (uint lId in lIds)
                {
                    IList<int> tmpCoIds = world.GetCoordIdsFromCadId(quantityId, lId, CadElementType.Loop);
                    foreach (int coId in tmpCoIds)
                    {
                        if (coIds.IndexOf(coId) == -1)
                        {
                            coIds.Add(coId);
                        }
                    }
                }
                // チャンネル
                {
                    IList<int> channelCoIds = new List<int>();
                    wgPortInfo.PCChannelCoIds.Add(channelCoIds);

                    int nodeId0 = wgPortInfo.BcNodess[0][0];
                    int coId0 = world.PortNode2Coord(quantityId, (uint)portId, nodeId0);
                    double[] coord0 = world.GetCoord(quantityId, coId0);
                    // 以下の処理はX方向周期構造の場合
                    System.Diagnostics.Debug.Assert(!wgPortInfo.IsYDirectionPeriodic);
                    foreach (int coId in coIds)
                    {
                        // 座標からチャンネル(欠陥部)を判定する
                        double[] coord = world.GetCoord(quantityId, coId);
                        // X方向周期構造
                        double y = Math.Abs(coord[1] - coord0[1]); // Y座標
                        //if (y >= (WaveguideWidth - rodDistanceY * (rodCntHalf + defectRodCnt) -
                        //    (0.5 * rodDistanceY - rodRadius)) && y <=
                        //    (WaveguideWidth - rodDistanceY * rodCntHalf +
                        //    (0.5 * rodDistanceY - rodRadius))) // dielectric rod
                        if (y >= (WaveguideWidth - rodDistanceY * (rodCntHalf + defectRodCnt) -
                            1.0 * rodDistanceY) &&
                            y <= (WaveguideWidth - rodDistanceY * rodCntHalf +
                            1.0 * rodDistanceY)) // air hole
                        {
                            channelCoIds.Add(coId);
                        }
                    }
                }
            }

            var chartWin = new ChartWindow();
            chartWin.Owner = mainWindow;
            chartWin.Left = mainWindow.Left + mainWindow.Width;
            chartWin.Top = mainWindow.Top;
            chartWin.Show();
            var model = new PlotModel();
            chartWin.plot.Model = model;
            model.Title = "PCWaveguide Example";
            var axis1 = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "a/λ",
                Minimum = sFreq,
                Maximum = eFreq
            };
            var axis2 = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "|S|",
                Minimum = 0.0,
                Maximum = 1.0
            };
            model.Axes.Add(axis1);
            model.Axes.Add(axis2);
            var series1 = new LineSeries
            {
                Title = "|S11|"
            };
            var series2 = new LineSeries
            {
                Title = "|S21|"
            };
            model.Series.Add(series1);
            model.Series.Add(series2);
            var datas = new List<DataPoint>();
            model.InvalidatePlot(true);
            WPFUtils.DoEvents();

            uint valueId = 0;
            var fieldDrawerArray = mainWindow.FieldDrawerArray;
            {
                world.ClearFieldValue();
                // 複素数
                valueId = world.AddFieldValue(FieldValueType.ZScalar, FieldDerivativeType.Value,
                    quantityId, false, FieldShowType.ZAbs);
                mainWindow.IsFieldDraw = true;
                fieldDrawerArray.Clear();
                IFieldDrawer faceDrawer = new FaceFieldDrawer(valueId, FieldDerivativeType.Value, true, world,
                    valueId, FieldDerivativeType.Value);
                fieldDrawerArray.Add(faceDrawer);
                IFieldDrawer edgeDrawer = new EdgeFieldDrawer(
                    valueId, FieldDerivativeType.Value, true, false, world);
                fieldDrawerArray.Add(edgeDrawer);
                mainWindow.Camera.Fit(fieldDrawerArray.GetBoundingBox(mainWindow.Camera.RotMatrix33()));
                mainWindow.glControl_ResizeProc();
                //mainWindow.glControl.Invalidate();
                //mainWindow.glControl.Update();
                //WPFUtils.DoEvents();
            }

            for (int iFreq = 0; iFreq < freqDiv + 1; iFreq++)
            {
                // a/λ
                double normalizedFreq = sFreq + (iFreq / (double)freqDiv) * (eFreq - sFreq);
                // 波長
                double waveLength = latticeA / normalizedFreq;
                // 周波数
                double freq = Constants.C0 / waveLength;
                System.Diagnostics.Debug.WriteLine("a/λ: " + normalizedFreq);

                var FEM = new PCWaveguide2DFEM(world);
                {
                    var solver = new IvyFEM.Linear.LapackEquationSolver();
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Dense;
                    solver.IsOrderingToBandMatrix = true;
                    solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.Band;
                    //solver.Method = IvyFEM.Linear.LapackEquationSolverMethod.PositiveDefiniteBand;
                    FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.LisEquationSolver();
                    //solver.Method = IvyFEM.Linear.LisEquationSolverMethod.Default;
                    //FEM.Solver = solver;
                }
                {
                    //var solver = new IvyFEM.Linear.IvyFEMEquationSolver();
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.COCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.ICCOCG;
                    //solver.Method = IvyFEM.Linear.IvyFEMEquationSolverMethod.NoPreconBiCGSTAB;
                    //FEM.Solver = solver;
                }
                FEM.WgPortInfos = wgPortInfos;
                FEM.Frequency = freq;
                FEM.Solve();
                System.Numerics.Complex[] Ez = FEM.Ez;
                System.Numerics.Complex[][] S = FEM.S;

                // eigen
                System.Numerics.Complex[][] portBetas = new System.Numerics.Complex[portCnt][];
                System.Numerics.Complex[][][] portEVecs = new System.Numerics.Complex[portCnt][][];
                for (int portId = 0; portId < portCnt; portId++)
                {
                    var eigenFEM = FEM.EigenFEMs[portId];
                    portBetas[portId] = eigenFEM.Betas;
                    portEVecs[portId] = eigenFEM.EVecs;
                }

                // eigen
                int targetModeId = 0;
                int nodeCnt = (int)world.GetNodeCount(quantityId);
                System.Numerics.Complex[] eigenEz = new System.Numerics.Complex[nodeCnt];
                for (int portId = 0; portId < portCnt; portId++)
                {
                    int modeCnt = portBetas[portId].Length;
                    if (targetModeId >= modeCnt)
                    {
                        System.Diagnostics.Debug.WriteLine("No defect mode found at port: " + portId);
                        continue;
                    }
                    System.Numerics.Complex beta = portBetas[portId][targetModeId];
                    System.Numerics.Complex[] eVec = portEVecs[portId][targetModeId];
                    int portNodeCnt = (int)world.GetPortNodeCount(quantityId, (uint)portId);
                    System.Diagnostics.Debug.Assert(portNodeCnt == eVec.Length);
                    for (int portNodeId = 0; portNodeId < portNodeCnt; portNodeId++)
                    {
                        int coId = world.PortNode2Coord(quantityId, (uint)portId, portNodeId);
                        int nodeId = world.Coord2Node(quantityId, coId);
                        System.Numerics.Complex value = eVec[portNodeId];
                        eigenEz[nodeId] = value;
                    }
                }

                if (S[0].Length > 0 && S[1].Length > 0)
                {
                    System.Numerics.Complex S11 = S[0][0];
                    System.Numerics.Complex S21 = S[1][0];
                    double S11Abs = S11.Magnitude;
                    double S21Abs = S21.Magnitude;
                    double total = S11Abs * S11Abs + S21Abs * S21Abs;

                    string ret;
                    string CRLF = System.Environment.NewLine;
                    ret = "2W/λ: " + normalizedFreq + CRLF;
                    ret += "|S11| = " + S11Abs + CRLF +
                          "|S21| = " + S21Abs + CRLF +
                          "|S11|^2 + |S21|^2 = " + total + CRLF;
                    System.Diagnostics.Debug.WriteLine(ret);
                    series1.Points.Add(new DataPoint(normalizedFreq, S11Abs));
                    series2.Points.Add(new DataPoint(normalizedFreq, S21Abs));
                    model.InvalidatePlot(true);
                    WPFUtils.DoEvents();
                }

                // eigen
                //world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, eigenEz);
                // Ez
                world.UpdateFieldValueValuesFromNodeValues(valueId, FieldDerivativeType.Value, Ez);

                fieldDrawerArray.Update(world);
                mainWindow.glControl.Invalidate();
                mainWindow.glControl.Update();
                WPFUtils.DoEvents();
            }
        }
    }
}
