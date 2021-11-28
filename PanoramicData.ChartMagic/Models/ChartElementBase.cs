namespace PanoramicData.ChartMagic.Models;

public abstract class ChartElementBase : IChartElement
{
	public IChartElement Parent { get; }

	/// <summary>
	/// False except for root elements
	/// </summary>
	public bool IsRoot { get; }

	protected ChartElementBase(IChartElement parent)
	{
		Parent = parent;
	}

	protected ChartElementBase()
	{
		Parent = this;
		IsRoot = true;
	}
}