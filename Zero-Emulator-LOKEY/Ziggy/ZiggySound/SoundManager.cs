//#define SOUND_SLIMDX

#if SOUND_SLIMDX
using SlimDX.XAudio2;
#else

using Microsoft.DirectX.DirectSound;
using System;
using System.Windows.Forms;

#endif

//using SlimDX.DirectSound;
namespace ZeroSound
{
    #region DirectSound

    public unsafe class SoundManager : System.IDisposable
    {
        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, System.ResolveEventArgs args) {
            string dllName = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");

            dllName = dllName.Replace(".", "_");

            if (dllName.EndsWith("_resources")) return null;

            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(typeof(SoundManager).Namespace + ".Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());

            byte[] bytes = (byte[])rm.GetObject(dllName);

            return System.Reflection.Assembly.Load(bytes);
        }

        private const int SAMPLE_SIZE = 882;
        private const int SAMPLE_RATE = 44100;
        private const int BUFFER_CHUNK = SAMPLE_RATE / 50;
        private const int SOUND_BUFFER_SIZE = BUFFER_CHUNK * 3;
        private const int BUFFER_COUNT = 4;
        public bool soundEnabled = true;
        private Device _device = null;
        private SecondaryBuffer _soundBuffer = null;
        private Notify _notify = null;
        public bool isPlaying = false;
        private byte _zeroValue;
        private int _bufferSize;
        private int _bufferCount;

        private System.Collections.Queue _fillQueue = null;
        private System.Collections.Queue _playQueue = null;
        private uint lastSample = 0;

        private System.Threading.Thread _waveFillThread = null;
        private System.Threading.AutoResetEvent _fillEvent = new System.Threading.AutoResetEvent(true);
        private bool _isFinished = false;
        private bool disposed = false;

		private bool _nosound;

        public SoundManager(System.IntPtr handle, short bitsPerSample, short channels, int samplesPerSecond)
		{
			string srcmethod = $"{nameof(SoundManager)}:CTOR";

            System.AppDomain.CurrentDomain.AssemblyResolve += new System.ResolveEventHandler(CurrentDomain_AssemblyResolve);
            _fillQueue = new System.Collections.Queue(BUFFER_COUNT);
            _playQueue = new System.Collections.Queue(BUFFER_COUNT);
            _bufferSize = SAMPLE_SIZE * 2 * 2;
            for (int i = 0; i < BUFFER_COUNT; i++)
                _fillQueue.Enqueue(new byte[_bufferSize]);

            _bufferCount = BUFFER_COUNT;
            _zeroValue = bitsPerSample == 8 ? (byte)128 : (byte)0;

			try
			{
				_device = new Device();
				_device.SetCooperativeLevel(handle, CooperativeLevel.Priority);
			}
			catch (Exception ex)
			{
				_device = null;
				_nosound = true;
				MessageBox.Show($"{srcmethod} : Exception creating DirectSound device {ex.ToString()}");
			}

			if (_nosound==false)
			{
				WaveFormat wf;

				try
				{
					wf = new WaveFormat();
					wf.FormatTag = WaveFormatTag.Pcm;
					wf.SamplesPerSecond = samplesPerSecond;
					wf.BitsPerSample = bitsPerSample;
					wf.Channels = channels;
					wf.BlockAlign = (short)(wf.Channels * (wf.BitsPerSample / 8));
					wf.AverageBytesPerSecond = (int)wf.SamplesPerSecond * (int)wf.BlockAlign;
				}
				catch (Exception ex)
				{
					MessageBox.Show($"{srcmethod} : Exception creating Waveformat {ex.ToString()}");
					throw new Exception($"{srcmethod} : Failed to create Waveformat");
				}

				try
				{
					// Create a buffer
					BufferDescription bufferDesc = new BufferDescription(wf);
					bufferDesc.BufferBytes = _bufferSize * _bufferCount;
					bufferDesc.ControlPositionNotify = true;
					bufferDesc.GlobalFocus = true;
					bufferDesc.ControlVolume = true;
					bufferDesc.ControlEffects = false;
					_soundBuffer = new SecondaryBuffer(bufferDesc, _device);

					_notify = new Notify(_soundBuffer);
					BufferPositionNotify[] posNotify = new BufferPositionNotify[_bufferCount];
					for (int i = 0; i < posNotify.Length; i++)
					{
						posNotify[i] = new BufferPositionNotify();
						posNotify[i].Offset = i * _bufferSize;
						posNotify[i].EventNotifyHandle = _fillEvent.SafeWaitHandle.DangerousGetHandle();
					}
					_notify.SetNotificationPositions(posNotify);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"{srcmethod} : Exception creating Buffer {ex.ToString()}");
					throw new Exception($"{srcmethod} : Failed to create Buffer");
				}

				try
				{
					_waveFillThread = new System.Threading.Thread(new System.Threading.ThreadStart(waveFillThreadProc));
					_waveFillThread.IsBackground = true;
					_waveFillThread.Name = "Wave fill thread";
					_waveFillThread.Priority = System.Threading.ThreadPriority.Highest;
					_waveFillThread.Start();
				}
				catch (Exception ex)
				{
					MessageBox.Show($"{srcmethod} : Exception creating waveFillThread {ex.ToString()}");
					throw new Exception($"{srcmethod} : Failed to create waveFillThread");
				}
			}
        }

        ~SoundManager()
        {
            Dispose(false);
        }

        public void Shutdown() {
            this.Dispose();
        }

        public void Reset() {
        }

        public void Play() {
            // _soundBuffer.Play(0, BufferPlayFlags.Looping);
        }

        public void PlayBuffer(ref short[] samples) {
        }

        public void Stop() {
            // _soundBuffer.Stop();
        }

        public bool FinishedPlaying()
		{
			if (_nosound == false)
			{
				lock (_fillQueue.SyncRoot)
					if (_fillQueue.Count < 1)
						return false;
			}
            return true;
        }

        public void SetVolume(float t)
		{
			if (_nosound == false)
			{
				if (t <= 0.0f)
					_soundBuffer.Volume = -10000;
				else if (t >= 1.0f)
					_soundBuffer.Volume = 0;
				else
					_soundBuffer.Volume = (int)(-2000.0f * System.Math.Log10(1.0f / t));
			}
        }

        public void Dispose() 
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (_waveFillThread != null)
                    {
                        try
                        {
                            _isFinished = true;
                            if (_soundBuffer != null)
                                if (_soundBuffer.Status.Playing)
                                    _soundBuffer.Stop();
                            _fillEvent.Set();

                            _waveFillThread.Join();

                            if (_soundBuffer != null)
                                _soundBuffer.Dispose();
                            if (_notify != null)
                                _notify.Dispose();

                            if (_device != null)
                                _device.Dispose();
                        }
                        catch (System.Threading.ThreadAbortException e)
                        {
                            System.Console.WriteLine("Sound thread exception " + e.Message);
                        }
                        finally
                        {
                            _waveFillThread = null;
                            _soundBuffer = null;
                            _notify = null;
                            _device = null;
                        }
                    }
                }
                // No unmanaged resources to release otherwise they'd go here.
            }
            disposed = true;
        }
        private unsafe void waveFillThreadProc()
		{
			if (_nosound == false)
			{
				int lastWrittenBuffer = -1;
				byte[] sampleData = new byte[_bufferSize];
				fixed (byte* lpSampleData = sampleData)
				{
					try
					{
						_soundBuffer.Play(0, BufferPlayFlags.Looping);
						while (!_isFinished)
						{
							_fillEvent.WaitOne();

							for (int i = (lastWrittenBuffer + 1) % _bufferCount; i != (_soundBuffer.PlayPosition / _bufferSize); i = ++i % _bufferCount)
							{
								OnBufferFill((System.IntPtr)lpSampleData, sampleData.Length);
								_soundBuffer.Write(_bufferSize * i, sampleData, LockFlag.None);
								lastWrittenBuffer = i;
							}
						}
					}
					catch (System.Exception ex)
					{
						System.Console.WriteLine("Sound thread exception " + ex.Message);
						//LogAgent.Error(ex);
					}
				}
			}
        }

        protected void OnBufferFill(System.IntPtr buffer, int length)
		{
			if (_nosound == false)
			{
				byte[] buf = null;
				lock (_playQueue.SyncRoot)
					if (_playQueue.Count > 0)
						buf = _playQueue.Dequeue() as byte[];
				if (buf != null)
				{
					uint* dst = (uint*)buffer;
					fixed (byte* srcb = buf)
					{
						uint* src = (uint*)srcb;
						for (int i = 0; i < length / 4; i++)
							dst[i] = src[i];
						lastSample = dst[length / 4 - 1];
					}
					lock (_fillQueue.SyncRoot)
						_fillQueue.Enqueue(buf);
				}
				else
				{
					uint* dst = (uint*)buffer;
					for (int i = 0; i < length / 4; i++)
						dst[i] = lastSample;
				}
			}
        }

        public byte[] LockBuffer()
		{
            byte[] sndbuf = null;
			if (_nosound == false)
			{
				lock (_fillQueue.SyncRoot)
					if (_fillQueue.Count > 0)
						sndbuf = _fillQueue.Dequeue() as byte[];
			}
            return sndbuf;
        }

        public void UnlockBuffer(byte[] sndbuf)
		{
			if (_nosound == false)
			{
				lock (_playQueue.SyncRoot)
					_playQueue.Enqueue(sndbuf);
			}
        }
    }
    #endregion DirectSound
}