using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.DirectInput;

namespace MagnusAkselvoll.AndroidPhotoBooth.Camera
{
    public sealed class JoystickInfo
    {
        public JoystickInfo(Joystick joystick)
        {
            Joystick = joystick;
        }

        public Joystick Joystick { get; }
        public Guid Id => Joystick.Information.InstanceGuid;
        public string Name => Joystick.Information.InstanceName;

        public static IReadOnlyCollection<JoystickInfo> All
        {
            get
            {
                var list = new List<JoystickInfo>();

                // Initialize DirectInput
                var directInput = new DirectInput();

                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                {
                    Guid gamepadGuid = deviceInstance.InstanceGuid;

                    list.Add(new JoystickInfo(new Joystick(directInput, gamepadGuid)));
                }

                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                {
                    Guid joystickGuid = deviceInstance.InstanceGuid;

                    list.Add(new JoystickInfo(new Joystick(directInput, joystickGuid)));
                }

                return list;
            }
        }

        public static JoystickInfo ConfiguredJoystick
        {
            get
            {
                return Properties.Settings.Default.Joystick == Guid.Empty ? null : All.FirstOrDefault(joystickInfo => joystickInfo.Id == Properties.Settings.Default.Joystick);
            }
        }
    }
}
