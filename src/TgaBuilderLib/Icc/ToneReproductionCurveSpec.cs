using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Icc;

public abstract record ToneReproductionCurveSpec
{
    public static ToneReproductionCurveSpec Para(int functionType, double[] parameters) =>
        new ParaTrc(functionType, parameters);

    public static ToneReproductionCurveSpec Identity() => new CurvTrc(CurvMode.Identity, 0.0, Array.Empty<ushort>());
    public static ToneReproductionCurveSpec Gamma(double g) => new CurvTrc(CurvMode.Gamma, g, Array.Empty<ushort>());
    public static ToneReproductionCurveSpec Table(ushort[] p) => new CurvTrc(CurvMode.Table, 0.0, p);

    public sealed record ParaTrc(int FunctionType, double[] Parameters) : ToneReproductionCurveSpec;
    public sealed record CurvTrc(CurvMode Mode, double GammaVal, ushort[] Points) : ToneReproductionCurveSpec;
}
