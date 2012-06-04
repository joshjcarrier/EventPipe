namespace EventPipe.Client.Netduino.Drivers
{
    using System;
    using Microsoft.SPOT.Hardware;

    /// <summary>
    /// Designed for use with TPIC6B595 shift register but may be compatible with other 595-series ICs.
    /// </summary>
    public class ShiftRegisterDriver : IDisposable
    {
        private readonly OutputPort latchPin;
        private readonly OutputPort clockPin;
        private readonly OutputPort dataPin;
        private readonly bool isLeastSignificantBitMode;
        private readonly int registerSize;
        
        public ShiftRegisterDriver(Cpu.Pin latchPin, Cpu.Pin clockPin, Cpu.Pin dataPin, bool isLeastSignificantBitMode, int registerSize)
        {
            this.latchPin = new OutputPort(latchPin, false);
            this.clockPin = new OutputPort(clockPin, false);
            this.dataPin = new OutputPort(dataPin, false);
            this.isLeastSignificantBitMode = isLeastSignificantBitMode;
            this.registerSize = registerSize;
        }

        public ShiftRegisterDriver(Cpu.Pin latchPin, Cpu.Pin clockPin, Cpu.Pin dataPin)
            : this(latchPin, clockPin, dataPin, false, 8)
        {
        }

        public Session AcquireSessionLock()
        {
            // TODO block or fail if open session count != 0
            return new Session(this);
        }

        public void Dispose()
        {
        }

        public class Session : IDisposable
        {
            private readonly ShiftRegisterDriver shiftRegisterDriver;

            public Session(ShiftRegisterDriver shiftRegisterDriver)
            {
                this.shiftRegisterDriver = shiftRegisterDriver;
            }

            public void Clear()
            {
                this.Write(0);
            }
            
            public void Write(byte value)
            {
                // latch pin low so can write data
                this.shiftRegisterDriver.latchPin.Write(false);

                for (var i = 0; i < this.shiftRegisterDriver.registerSize; i++)
                {
                    byte mask;
                    if (this.shiftRegisterDriver.isLeastSignificantBitMode)
                    {
                        mask = (byte)(1 << i);
                    }
                    else
                    {
                        mask = (byte)(1 << (this.shiftRegisterDriver.registerSize - 1 - i));
                    }

                    // Write data
                    this.shiftRegisterDriver.dataPin.Write((value & mask) != 0);

                    // Raise Clock to indicate write bit has been set
                    this.shiftRegisterDriver.clockPin.Write(true);
                    
                    // Lower Clock to prepare for next write
                    this.shiftRegisterDriver.clockPin.Write(false);
                }

                // latch pin high so can commit data
                this.shiftRegisterDriver.latchPin.Write(true);
            }
            
            public void Dispose()
            {
            }
        }
    }
}
