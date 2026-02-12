using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Utility
{
    public class XYZCalc
    {
        /// <summary>
        /// 常用参考白点 (D65_10C 光源)
        /// </summary>
        public double[] ReferenceWhiteXYZ { get; set; } = [94.81, 100.000, 107.32];
        /// <summary>
        /// 预定义光学参数类型
        /// </summary>
        public CmfType CmfTypeVal { get; }
        #region XYZ
        public double[] Wavelengths { get; }
        public double[] LightSPD { get; }
        public double[] CmfX { get; }
        public double[] CmfY { get; }
        public double[] CmfZ { get; }
        /// <summary>
        /// 默认 CIE1964_10纳米_10度_D65光源计算
        /// </summary>
        public XYZCalc():this(CmfType.R380_780_10)
        {
        }
        public enum CmfType
        {
            
            R380_780_10 = 0,
            R400_700_10 = 1
        }
        public XYZCalc(CmfType type) {

            CmfTypeVal = type;
            if (type == CmfType.R380_780_10)
            {
                Wavelengths = CIE1964_10n_10C_D65.Select(p => p[0]).ToArray();
                LightSPD = CIE1964_10n_10C_D65.Select(p => p[4]).ToArray();
                CmfX = CIE1964_10n_10C_D65.Select(p => p[1]).ToArray();
                CmfY = CIE1964_10n_10C_D65.Select(p => p[2]).ToArray();
                CmfZ = CIE1964_10n_10C_D65.Select(p => p[3]).ToArray();
            }
            else if (type == CmfType.R400_700_10) {
                var count = 31;
                Wavelengths = CIE1964_10n_10C_D65.Select(p => p[0]).Skip(2).Take(count).ToArray();
                LightSPD = CIE1964_10n_10C_D65.Select(p => p[4]).Skip(2).Take(count).ToArray();
                CmfX = CIE1964_10n_10C_D65.Select(p => p[1]).Skip(2).Take(count).ToArray();
                CmfY = CIE1964_10n_10C_D65.Select(p => p[2]).Skip(2).Take(count).ToArray();
                CmfZ = CIE1964_10n_10C_D65.Select(p => p[3]).Skip(2).Take(count).ToArray();
            }
                
        }
        /// <summary>
        /// 根据光谱反射率数据计算CIE XYZ三刺激值
        /// </summary>
        /// <param name="wavelengths">波长数组（单位：nm）</param>
        /// <param name="reflectances">对应波长的反射率数组（范围0-1）</param>
        /// <param name="lightSPD">光源的光谱功率分布数组（与wavelengths对应）</param>
        /// <param name="cmfX">CIE标准观察者颜色匹配函数X̄（与wavelengths对应）</param>
        /// <param name="cmfY">CIE标准观察者颜色匹配函数Ȳ（与wavelengths对应）</param>
        /// <param name="cmfZ">CIE标准观察者颜色匹配函数Z̄（与wavelengths对应）</param>
        /// <returns>包含X, Y, Z值的数组</returns>
        public static double[] CalcXYZFromSpectralReflectance(
            double[] wavelengths,
            double[] reflectances,
            double[] lightSPD,
            double[] cmfX,
            double[] cmfY,
            double[] cmfZ)
        {
            // 参数校验
            if (wavelengths == null || reflectances == null || lightSPD == null ||
                cmfX == null || cmfY == null || cmfZ == null)
            {
                throw new ArgumentNullException("输入参数不能为null");
            }

            int length = wavelengths.Length;
            if (reflectances.Length != length || lightSPD.Length != length ||
                cmfX.Length != length || cmfY.Length != length || cmfZ.Length != length)
            {
                throw new ArgumentException("所有输入数组的长度必须相同");
            }

            double sumX = 0.0, sumY = 0.0, sumZ = 0.0;
            double sumYNormalization = 0.0;

            // 计算加权求和
            for (int i = 0; i < length; i++)
            {
                // 计算积分项：光源SPD * 颜色匹配函数 * 反射率
                double integrandX = lightSPD[i] * cmfX[i] * reflectances[i];
                double integrandY = lightSPD[i] * cmfY[i] * reflectances[i];
                double integrandZ = lightSPD[i] * cmfZ[i] * reflectances[i];

                sumX += integrandX;
                sumY += integrandY;
                sumZ += integrandZ;

                // 为归一化系数k计算分母（对完全漫反射体Y的求和）
                sumYNormalization += lightSPD[i] * cmfY[i];
            }

            // 计算归一化系数k，使得对于完全漫反射体Y=100
            double k = 100.0 / sumYNormalization;

            // 应用归一化系数
            double X = k * sumX;
            double Y = k * sumY;
            double Z = k * sumZ;

            return [X, Y, Z];
        }



        /// <summary>
        /// xyz计算,根据CmfType，默认380~780纳米间隔10的
        /// </summary>
        /// <param name="reflectances"></param>
        /// <returns></returns>
        public double[] CalcXyzByR(double[] reflectances)
        {
            return CalcXYZFromSpectralReflectance(Wavelengths, reflectances, LightSPD, CmfX, CmfY, CmfZ);
        }

        public double[] XyzToLab(double x, double y, double z)
        {
            return XyzToLab(x, y, z,ReferenceWhiteXYZ);
        }
        public double[] LabToXyz(double L, double a, double b)
        {
            return LabToXyz(L, a, b, ReferenceWhiteXYZ);
        }
        /// <summary>
        ///  XYZ0转换
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static double[] XyzTo0(double[] xyz)
        {
            var sum = xyz[0] + xyz[1] + xyz[2];
            return [xyz[0] / sum, xyz[1] / sum, xyz[2] / sum];
        }
     
       
        /// <summary>
        /// CIE1964_10纳米_10度_D65光源：n_XYZ_D
        /// </summary>
        static readonly double[][] CIE1964_10n_10C_D65 =
      [
             [380,0.0002,0,0.0007,49.98]
            ,[390,0.0024,0.0003,0.0105,54.65]
            ,[400,0.0191,0.002,0.086,82.75]
            ,[410,0.0847,0.0088,0.3894,91.49]
            ,[420,0.2045,0.0214,0.9725,93.43]
            ,[430,0.3147,0.0387,1.5535,86.68]
            ,[440,0.3837,0.0621,1.9673,104.86]
            ,[450,0.3707,0.0895,1.9948,117.01]
            ,[460,0.3023,0.1282,1.7454,117.81]
            ,[470,0.1956,0.1852,1.3176,114.86]
            ,[480,0.0805,0.2536,0.7721,115.92]
            ,[490,0.0162,0.3391,0.4153,108.81]
            ,[500,0.0038,0.4608,0.2185,109.35]
            ,[510,0.0375,0.6067,0.112,107.8]
            ,[520,0.1177,0.7618,0.0607,104.79]
            ,[530,0.2365,0.8752,0.0305,107.69]
            ,[540,0.3768,0.962,0.0137,104.41]
            ,[550,0.5298,0.9918,0.004,104.05]
            ,[560,0.7052,0.9973,0,100]
            ,[570,0.8787,0.9556,0,96.33]
            ,[580,1.0142,0.8689,0,95.79]
            ,[590,1.1185,0.7774,0,88.69]
            ,[600,1.124,0.6583,0,90.01]
            ,[610,1.0305,0.528,0,89.6]
            ,[620,0.8563,0.3981,0,87.7]
            ,[630,0.6475,0.2835,0,83.29]
            ,[640,0.4316,0.1798,0,83.7]
            ,[650,0.2683,0.1076,0,80.03]
            ,[660,0.1526,0.0603,0,80.21]
            ,[670,0.0813,0.0318,0,82.28]
            ,[680,0.0409,0.0159,0,78.28]
            ,[690,0.0199,0.0077,0,69.72]
            ,[700,0.0096,0.0037,0,71.61]
            ,[710,0.0046,0.0018,0,74.35]
            ,[720,0.0022,0.0008,0,61.6]
            ,[730,0.001,0.0004,0,69.89]
            ,[740,0.0005,0.0002,0,75.09]
            ,[750,0.0003,0.0001,0,63.59]
            ,[760,0.0001,0,0,46.42]
            ,[770,0.0001,0,0,66.81]
            ,[780,0,0,0,63.38]
      ];
        #endregion

        #region 457b
        /// <summary>
        /// 直接取460纳米反射率
        /// </summary>
        /// <param name="reflectances"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public double Get457b(double[] reflectances) {

            if (reflectances.Length != Wavelengths.Length)
                throw new ArgumentException("数组长度不符");
            if (CmfTypeVal == CmfType.R380_780_10) {
                return reflectances[8];
            }
            else if(CmfTypeVal==CmfType.R400_700_10)
            {
                return reflectances[6];
            }
            throw new ArgumentException("未知类型");
        }
        #endregion

        #region Yellow index calc
        public static double YellownessIndexByD65c10(double[] xyz)
        {
            var d65Cx = 1.3013;
            var d65Cz = 1.1498;
            var yi = 100 * (d65Cx * xyz[0] - d65Cz * xyz[2]) / xyz[1];
            return yi;
        }

        /// <summary>
        /// 白度
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static double WhitenessIndexByD65c10(double[] xyz) {
            double Xn = 0.3138,yn=0.331,WI_x = 800,WI_y = 1700;
            var xyz0=XyzTo0(xyz);
            var WI=xyz[0] + WI_x * (Xn - xyz0[0]) + WI_y * (yn - xyz0[1]);
            return WI;
        }
        #endregion

        #region LAB

       const double labTransformConst= 0.206893;// =6.0 / 29.0;
        /// <summary>
        /// 将 CIE XYZ 颜色值转换为 CIE LAB 颜色值。
        /// </summary>
        /// <param name="x">X 分量</param>
        /// <param name="y">Y 分量</param>
        /// <param name="z">Z 分量</param>
        /// <param name="referenceWhiteXYZ">参考白点XYZ</param>>
        public static double[] XyzToLab(double x, double y, double z, double[] referenceWhiteXYZ)
        {
            // 1. 使用参考白点 (D65) 进行归一化
            double xNormalized = x / referenceWhiteXYZ[0];
            double yNormalized = y / referenceWhiteXYZ[1];
            double zNormalized = z / referenceWhiteXYZ[2];

            // 2. 应用非线性变换函数 LabTranFun
            const double deltaCubed = labTransformConst * labTransformConst * labTransformConst; // ≈ 0.008856
            const double scale = 1.0 / (3.0 * labTransformConst * labTransformConst); // ≈ 7.787037
            const double divide1by3 = 1 / 3d;
            const double divide16by116 = 16 / 116d;
            double fx = xNormalized > deltaCubed ? Math.Pow(xNormalized, divide1by3) : xNormalized * scale + divide16by116;// LabTranFun(xNormalized);
            double fy = yNormalized > deltaCubed ? Math.Pow(yNormalized, divide1by3) : yNormalized * scale + divide16by116; ;
            double fz = zNormalized > deltaCubed ? Math.Pow(zNormalized, divide1by3) : zNormalized * scale + divide16by116; ;

            // 3. 计算 LAB 值

            var labL = 116.0 * fy - 16.0;
            var labA = 500.0 * (fx - fy);
            var labB = 200.0 * (fy - fz);
            return [labL, labA, labB];
        }
        /// <summary>
        /// Lab颜色值转XYZ
        /// </summary>
        /// <param name="L">L*值 (0-100)</param>
        /// <param name="a">a*值 (通常-128到127)</param>
        /// <param name="b">b*值 (通常-128到127)</param>
        /// <param name="referenceWhite">参考白点，默认为D65</param>
        /// <returns>XYZ值元组</returns>
        public static double[] LabToXyz(
            double L, double a, double b,
            double[] referenceWhiteXYZ)
        {
            const double scale = 1.0 / (3.0 * labTransformConst * labTransformConst);
            const double scale116 = scale * 116; //903.328298;
            // 步骤1: 计算fY, fX, fZ
            double fy = (L + 16.0) / 116.0;
            double fx = fy + a / 500.0;
            double fz = fy - b / 200.0;

            // 步骤2: 反变换计算Xr, Yr, Zr（相对于参考白点）
            double xr = fx > labTransformConst ? fx * fx * fx : (116.0 * fx - 16.0) / scale116;
            double yr = fy > labTransformConst ? Math.Pow(fy, 3) : L / scale116;
            double zr = fz > labTransformConst ? fx * fz * fz : (116.0 * fz - 16.0) / scale116; ;

            // 步骤3: 转换为绝对XYZ值
            double X = xr * referenceWhiteXYZ[0];
            double Y = yr * referenceWhiteXYZ[1];
            double Z = zr * referenceWhiteXYZ[2];

            return [X, Y, Z];
        }

        /// <summary>
        /// 计算两个XYZ颜色的色差值ΔE
        /// </summary>
        /// <param name="standardXyz">标准色XYZ值</param>
        /// <param name="sampleXyz">样品色XYZ值</param>
        /// <param name="deltaEType">ΔE计算标准</param>
        /// <returns>色差值（越大差异越明显）</returns>
        public static double DeltaE((double L, double a, double b) standard,
            (double L, double a, double b) lab)
        {
            double deltaL = lab.L - standard.L;
            double deltaA = lab.a - standard.a;
            double deltaB = lab.b - standard.b;
            return Math.Sqrt(deltaL * deltaL + deltaA * deltaA + deltaB * deltaB);
        }

        /// <summary>
        /// ΔE₇₆（基础版）
        /// </summary>
        private static double CalculateDeltaE76((double L, double a, double b) lab1, (double L, double a, double b) lab2)
        {
            double deltaL = lab2.L - lab1.L;
            double deltaA = lab2.a - lab1.a;
            double deltaB = lab2.b - lab1.b;
            return Math.Sqrt(deltaL * deltaL + deltaA * deltaA + deltaB * deltaB);
        }
        #endregion
        #region LCH

        public static double[] LabToLch(double[] lab)
        {
            var aStar = lab[1];
            var bStar = lab[2];
            // 2. 然后将 Lab 转换为 LCH
            var C = Math.Sqrt(aStar * aStar + bStar * bStar); // 计算彩度

            // 计算色调角 (弧度)，并使用 Atan2 处理所有象限
            double hRadians = Math.Atan2(bStar, aStar);
            // 将弧度转换为角度，并确保范围在 [0, 360) 之间
            var H = hRadians * (180.0 / Math.PI);
            if (H < 0) H += 360.0;

            return new[] { lab[0], C, H };
        }
        #endregion
        #region sRGB

        private static readonly double[,] sRGB_Matrix_D65_10deg = {
            { 3.133336, -1.616700, -0.490614 },
            {-0.978768,  1.916141,  0.033454 },
            { 0.071945, -0.228991, 1.405242 }
        };
        private static readonly double[,] sRGB_Matrix_D65_2deg =
        {
            { 3.2404542, -1.5371385, -0.4985314 },
            { -0.9692660, 1.8760108,  0.0415560 },
            { 0.0556434, -0.2040259,  1.0572252 }
        };
        public byte[]? XyzToSrgb(double X, double Y, double Z)
        {
            return XyzToSrgb(X, Y, Z, ReferenceWhiteXYZ[0], ReferenceWhiteXYZ[1], ReferenceWhiteXYZ[2],sRGB_Matrix_D65_2deg);
        }
        /// <summary>
        /// 将 CIE XYZ 颜色值（使用指定的 D65 参考白点）转换为 sRGB 颜色值。
        /// 此方法将 D65 光源的白点坐标作为参数，提高了灵活性。
        /// </summary>
        /// <param name="X">输入的 X 三刺激值</param>
        /// <param name="Y">输入的 Y 三刺激值 (通常范围 0-100)</param>
        /// <param name="Z">输入的 Z 三刺激值</param>
        /// <param name="Xn">D65 参考白点的 X 分量 (如 ColorSpaceConverter.D65_Xn_10deg)</param>
        /// <param name="Yn">D65 参考白点的 Y 分量 (如 ColorSpaceConverter.D65_Yn_10deg, 通常为 100)</param>
        /// <param name="Zn">D65 参考白点的 Z 分量 (如 ColorSpaceConverter.D65_Zn_10deg)</param>
        /// <returns>成功返回[rgb],失败返回null</returns>
        public static byte[]? XyzToSrgb(double X, double Y, double Z,
                                               double Xn, double Yn, double Zn, double[,] conversionMatrix)
        {
            // 输入验证
            if (double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z) ||
                double.IsNaN(Xn) || double.IsNaN(Yn) || double.IsNaN(Zn) ||
                Yn <= 0) // Yn 通常为 100，必须为正数以避免除零错误
            {
                return null;
            }

            try
            {
                // 1. 使用参考白点进行归一化
                double xNormalized = X / 100;// Xn;
                double yNormalized = Y / 100;// Yn;
                double zNormalized = Z / 100;// Zn;

                // 2. 线性变换：使用 XYZ (D65) 到 线性 sRGB 的转换矩阵
                double rLinear = (conversionMatrix[0,0] * xNormalized) + (conversionMatrix[0, 1] * yNormalized) + (conversionMatrix[0, 2] * zNormalized);
                double gLinear = (conversionMatrix[1, 0] * xNormalized) + (conversionMatrix[1, 1] * yNormalized) + (conversionMatrix[1, 2] * zNormalized);
                double bLinear = (conversionMatrix[2, 0] * xNormalized) + (conversionMatrix[2, 1] * yNormalized) + (conversionMatrix[2, 2] * zNormalized);

                // 3. 伽马校正（sRGB 压缩/编码）
                rLinear = ApplySrgbGammaCorrection(rLinear);
                gLinear = ApplySrgbGammaCorrection(gLinear);
                bLinear = ApplySrgbGammaCorrection(bLinear);

                // 4. 钳制到 [0, 1] 范围并缩放到 [0, 255]
                var rgb = new byte[3];
                rgb[0] = (byte)(255 * Clamp(rLinear, 0.0, 1.0));
                rgb[1] = (byte)(255 * Clamp(gLinear, 0.0, 1.0));
                rgb[2] = (byte)(255 * Clamp(bLinear, 0.0, 1.0));

                return rgb;
            }
            catch
            {
                // 处理计算过程中可能出现的异常（如数值溢出）
                return null;
            }
        }

        /// <summary>
        /// 应用 sRGB 伽马校正函数（逆伽马变换，从线性值到编码值）。
        /// </summary>
        private static double ApplySrgbGammaCorrection(double linearComponent)
        {
            return linearComponent <= 0.0031308 ? 12.92 * linearComponent : (1.055 * Math.Pow(linearComponent, 1.0 / 2.4) - 0.055);
        }

        /// <summary>
        /// 将值限制在指定范围内。
        /// </summary>
        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }
        #endregion
    }
}
