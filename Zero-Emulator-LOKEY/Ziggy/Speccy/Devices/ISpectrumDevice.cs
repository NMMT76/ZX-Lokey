using SpeccyCommon;

namespace Speccy
{
    public interface ISpectrumDevice
    {
        void RegisterDevice(zx_spectrum hostmachine);
        void UnregisterDevice(zx_spectrum hostmachine);
        void Reset();
        SPECTRUM_DEVICE DeviceID { get; }
    }
}
