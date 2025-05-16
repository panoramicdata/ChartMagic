namespace PanoramicData.ChartMagic.Models;

public class AnnotationCollection(IChartElement parent, IList<Annotation> list) : ChartNamedElementCollection<Annotation>(parent, list)
{
}
