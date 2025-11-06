public interface ITemperatureAffectable : IHeatable, IFreezable
{
    float CurrentTemperature { get; }
}
