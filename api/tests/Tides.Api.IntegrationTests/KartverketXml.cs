namespace Tides.Api.IntegrationTests;

public static class KartverketXml
{
    public const string Tides = """
        <tide><locationdata><data>
            <waterlevel value="250.0" time="2026-11-01T00:05:00+00:00" flag="high"/>
        </data></locationdata></tide>
    """;

    public const string Predictions = """
        <tide><locationdata><data>
            <waterlevel value="248.0" time="2026-11-01T00:00:00+00:00" flag="pre"/>
            <waterlevel value="250.0" time="2026-11-01T00:10:00+00:00" flag="pre"/>
            <waterlevel value="249.0" time="2026-11-01T00:20:00+00:00" flag="pre"/>
        </data></locationdata></tide>
    """;

    public const string NoData = """
        <tide>
            <locationdata>
                <nodata info="Sorry. There are no sea level data for N61°54.0' E10°16.8' because N61°54.0' E10°16.8' is too far away from the coast."/>
            </locationdata>
        </tide>
    """;
}
