using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Devices.Hardware.Interfaces;
using Devices.Util.Extensions;

namespace Devices.Hardware.Actors
{
    public class Servo
    {
        public const int DefaultServoFrequency = 50;
        public const int DefaultRampIntervalMs = 20;
        public const double DefaultRampStep = 1.0;

        private IPWMChannel pwmChannel;
        private double position;
        private double minAngle;
        private double maxAngle;
        private double scale;   //Ratio max-min Pulsewidth / max-min Angle
        private double offset;  //equals min pulsewidth
        private bool limitsSet; 

        //Ramping (gradually moving to new positon)
        private AutoResetEvent waitEvent;
        private bool synchronized;
        private Timer rampTimer;
        private double rampStep = DefaultRampStep;
        private int rampInterval = DefaultRampIntervalMs;
        private double rampTarget;


        public Servo(IPWMChannel pwmChannel)
        {
            this.pwmChannel = pwmChannel;
            this.rampTimer = new Timer(this.OnRampTimer, null, Timeout.Infinite, rampInterval);
            this.synchronized = true;
            if (synchronized)
                this.waitEvent = new AutoResetEvent(false);
        }


        public double Position
        {
            get
            {
                return this.position;
            }
            set
            {
                if (!this.limitsSet) throw new InvalidOperationException($"You must call {nameof(this.SetLimits)} first.");
                if (value < this.minAngle || value > this.maxAngle) throw new ArgumentOutOfRangeException(nameof(value));
                if (value == position)
                    return;

                if (0 < rampInterval  && 0 != rampStep)
                {
                    rampTarget = value;
                    rampStep = (System.Math.Sign(rampTarget - this.position) * DefaultRampStep);
                    this.rampTimer.Change(0, rampInterval);
                    if (this.synchronized)
                        waitEvent.WaitOne();
                }
                else
                {
                    SetPosition(value);
                }
                this.position = value;
            }
        }

        public int RampInterval
        {
            get { return this.rampInterval; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(RampInterval));
                rampInterval = value;
            }
        }

        public double RampStep
        {
            get { return this.rampStep; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(RampStep));
                this.rampStep = value;
            }
        }

        public bool SynchronizeMovement
        {
            get { return this.synchronized; }
            set { this.synchronized = value; }
        }

        private void OnRampTimer(object state)
        {
            SetPosition(this.position + rampStep);
            if (rampStep > 0 ? (this.position + rampStep > rampTarget) : this.position + rampStep < rampTarget) //in range to overrun the target
            {
                this.rampTimer.Change(Timeout.Infinite, rampInterval);
                SetPosition(rampTarget);
                if (this.synchronized)
                    this.waitEvent.Set();
            }
        }

        private void SetPosition(double position)
        {
            this.position = position;
            this.pwmChannel.SetPulse((ushort)(this.scale * position + this.offset));
        }

        public void Disengage()
        {
            this.pwmChannel.Release();
        }

        public void Engage()
        {
            SetPosition(this.position);
        }

        public void SetLimits(int minimumPulseWidth, int maximumPulseWidth, double minimumAngle, double maximumAngle)
        {
            SetLimits(minimumPulseWidth, maximumPulseWidth, minimumAngle, maximumAngle, (minimumAngle + maximumAngle / 2));
        }

        /// <summary>
        /// Sets the limits of the servo.
        /// </summary>
        /// <param name="minimumPulseWidth">The minimum pulse width in milliseconds.</param>
        /// <param name="maximumPulseWidth">The maximum pulse width in milliseconds.</param>
        /// <param name="minimumAngle">The minimum angle of input passed to Position.</param>
        /// <param name="maximumAngle">The maximum angle of input passed to Position.</param>
        /// <param name="initialPosition">Initial position of the servo</param>
        public void SetLimits(int minimumPulseWidth, int maximumPulseWidth, double minimumAngle, double maximumAngle, double initialPosition)
        {
            if (minimumPulseWidth < 0) throw new ArgumentOutOfRangeException(nameof(minimumPulseWidth));
            if (maximumPulseWidth < 0) throw new ArgumentOutOfRangeException(nameof(maximumPulseWidth));
            if (minimumAngle < 0) throw new ArgumentOutOfRangeException(nameof(minimumAngle));
            if (maximumAngle < 0) throw new ArgumentOutOfRangeException(nameof(maximumAngle));
            if (minimumPulseWidth >= maximumPulseWidth) throw new ArgumentException(nameof(minimumPulseWidth));
            if (minimumAngle >= maximumAngle) throw new ArgumentException(nameof(minimumAngle));

            if (this.pwmChannel.Frequency != DefaultServoFrequency)
                this.pwmChannel.Frequency = DefaultServoFrequency;

            this.minAngle = minimumAngle;
            this.maxAngle = maximumAngle;

            double period = 1000000.0 / DefaultServoFrequency; 

            minimumPulseWidth = (int)(minimumPulseWidth / period * pwmChannel.Resolution);
            maximumPulseWidth = (int)(maximumPulseWidth / period * pwmChannel.Resolution);

            this.scale = ((maximumPulseWidth - minimumPulseWidth) / (maximumAngle - minimumAngle));
            this.offset = minimumPulseWidth;
            this.position = initialPosition;

            this.limitsSet = true;

            ///"Smoothly" set the servo to an initial position
            ///Since there's no way to get the current position from Servo itself,
            ///we enable the servo for very short periods to allow some small movements
            ///500 iterations seems to be enough to cover a full range (if servo is at one end and inital position is on the other end)
            for (int i = 0; i < 500; i++)
            {
                SetPosition(position);
                Disengage();
                SpinWaitExtension.SpinFor(TimeSpan.FromMilliseconds(1));
            }
            //Finally set and ensure the initial position
            SetPosition(position);
        }


    }
}
