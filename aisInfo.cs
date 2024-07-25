using System;
using canlibCLSNET;
using System.Device.Location;
using Microsoft.SqlServer.Server;
using System.Collections.Generic;

namespace AIS_Read
{
    public class AIS_Read
    {
        public static GeoCoordinate _AISGeoCoordinate { get; set; } = new GeoCoordinate();
        public static double AIS_Angle { get; set; }
        public static double AIS_SOG { get; set; }
        public static string _aiscallSign { get; set; }
        public static string _aisName { get; set; }
        public static uint ais_mmsi { get; set; }
        public static string ais_positionAccuracy { get; set; }
        public static decimal ais_IMO { get; set; }
        public static string cargoShipType { get; set; }
        public static double _shipLength { get; set; }
        public static double _shipBeam { get; set; }
        public static double _shipDraft { get; set; }
        public static string _aisNavstatus { get; set; }
        public static int _aisRateOfTurn { get; set; }
        private static byte firstByte;
        private static byte imoByte;

        private static string result;
        private static string result1;
        private static string result2;
        private static string result3;

        // Add a method to get data for all vessels
        public static List<AisData> GetAllAisData()
        {
            // Replace this with your actual logic to fetch AIS data for all vessels
            return new List<AisData>
            {
                new AisData { aisName = _aisName, aisLatitude = _AISGeoCoordinate.Latitude, aisLongitude = _AISGeoCoordinate.Longitude, aisAngle = AIS_Angle, aisSOG = AIS_SOG},
                // Add more vessels here
            };
        }
        public class AisData
        {
            public string aisName { get; set; }
            public double aisLatitude { get; set; }
            public double aisLongitude { get; set; }
            public double aisAngle { get; set; }
            public double aisSOG { get; set; }
        }
        public enum AIS_Ship_Cargo_Type
        {
            NOT_AVAILABLE = 0,
            RESERVED_FOR_FUTURE = 1,
            WIG_CRAFT = 2,
            OTHER_VESSELS = 3,
            HSC_PASSENGER_FERRIES = 4,
            SPECIAL_CRAFT = 5,
            PASSENGER_SHIPS = 6,
            CARGO_SHIPS = 7,
            TANKERS = 8,
            OTHER_TYPES = 9,
            FISHING_VESSEL = 30,
            TOWING_VESSEL_AHEAD_SIDE = 31,
            TOWING_VESSEL_ASTERN = 32,
            DREDGING_VESSEL = 33,
            DIVING_VESSEL = 34,
            MILITARY_VESSEL = 35,
            SAILING_VESSEL = 36,
            PLEASURE_CRAFT = 37,
            PILOT_VESSEL = 50,
            RESCUE_VESSEL = 51,
            TUGS = 52,
            PORT_TENDERS = 53,
            ANTI_POLLUTION_VESSEL = 54,
            LOW_ENFORCEMENTT_VESSEL = 55,
            MEDICAL_TRANSPORTS = 58,
            RR_RESOLUTION_VESSEL = 59
        }

        public enum AIS_Navigation_Status
        {
            UNDER_WAY_USING_ENGINE,
            AT_ANCHOR,
            NOT_UNDER_COMMAND,
            RESTRICTED_MANOEUVRABILITY,
            CONSTRAINED_BY_HER_DRAUGHT,
            MOORED,
            AGROUND,
            ENGAGED_IN_FISHING,
            UNDER_WAY_SAILING,
            RESERVED1,
            RESERVED2,
            RESERVED_FOR_FUTURE1,
            RESERVED_FOR_FUTURE2,
            RESERVED_FOR_FUTURE3,
            RESERVED_FOR_FUTURE4,
            NOT_DEFINED,
        }
        private static int Bin_Hex(string bin)
        {
            int hex = Convert.ToInt32(bin, 2);
            return hex;
        }
        private static uint largeCombine(byte b1, byte b2, byte b3, byte b4)
        {
            uint combinedValue = (uint)((b1) | (b2 << 8) | (b3 << 16) | (b4 << 24));
            return combinedValue;
        }
        private static decimal Combine(byte b1, byte b2, byte b3, byte b4)
        {
            return (b4 << 24) | (b3 << 16) | (b2 << 8) | b1;
        }
        private static double Mix(byte b1, byte b2)
        {
            return (b2 << 8) | b1;
        }
        public static double ToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        public static void DumpaisMessageLoop()
        {
            int handle;
            Canlib.canStatus status;
            int channelNumber = 2;

            Canlib.canInitializeLibrary();

            handle = Canlib.canOpenChannel(channelNumber, Canlib.canOPEN_ACCEPT_VIRTUAL);
            status = Canlib.canSetBusParams(handle, Canlib.canBITRATE_250K, 0, 0, 0, 0, 0);
            status = Canlib.canBusOn(handle);

            byte[] data = new byte[8];
            int id;
            int dlc;
            int flags;
            long timestamp;

            while (true)
            {
                status = Canlib.canReadWait(handle, out id, data, out dlc, out flags, out timestamp, 100);

                string idBinary = Convert.ToString(id, 2).PadLeft(29, '0');

                string hex1 = idBinary.Substring(0, 3);
                string hex2 = idBinary.Substring(3, 18);
                string hex3 = idBinary.Substring(21, 8);

                int h1 = Bin_Hex(hex1);
                int h2 = Bin_Hex(hex2);
                int h3 = Bin_Hex(hex3);
                if (status == Canlib.canStatus.canOK)
                {
                    if (h2 == 129038)
                    {
                        if ((data[0] >= 0 || data[0] <= 224) && data[0] % 32 == 0)
                        {
                            firstByte = data[7];
                            ais_mmsi = largeCombine(data[3], data[4], data[5], data[6]);
                        }
                        if (((data[0] - 1) >= 0 || (data[0] - 1) <= 224) && (data[0] - 1) % 32 == 0)
                        {
                            _AISGeoCoordinate.Latitude = Convert.ToDouble(Combine(data[4], data[5], data[6], data[7]) / 10000000m);
                            _AISGeoCoordinate.Longitude = Convert.ToDouble(Combine(firstByte, data[1], data[2], data[3]) / 10000000m);
                        }
                        if (((data[0] - 2) >= 0 || (data[0] - 2) <= 224) && (data[0] - 2) % 32 == 0)
                        {
                            if (data[1] == 0)
                            {
                                ais_positionAccuracy = "Low >=10m";
                            }
                            else if (data[1] == 1)
                            {
                                ais_positionAccuracy = "High <10m";
                            }
                            AIS_SOG = Mix(data[4], data[5]) * 2 / 100.0;

                            AIS_Angle = Mix(data[2], data[3]);
                            AIS_Angle = Math.Round(ToDegrees(AIS_Angle / 10000.0), 1);
                        }
                        if (((data[0] - 3) >= 0 || (data[0] - 3) <= 224) && (data[0] - 3) % 32 == 0)
                        {
                            var rot = Mix(data[4], data[5]) * 0.001 * (1.0 / 32.0);
                            _aisRateOfTurn = (int)Math.Round(rot * (180.0 / Math.PI) * 60);
                            byte[] bytes = new byte[] { data[6], data[7] };
                            int result = ((bytes[0] << 8) | bytes[1]) & bytes[0];  // Combine bytes and mask the lower 8 bits

                            AIS_Navigation_Status _status;

                            if (Enum.IsDefined(typeof(AIS_Navigation_Status), result))
                            {
                                _status = (AIS_Navigation_Status)result;
                            }
                            else
                            {
                                _status = AIS_Navigation_Status.NOT_DEFINED;
                            }

                            // Get the enum name and replace underscores with spaces
                            _aisNavstatus = Enum.GetName(typeof(AIS_Navigation_Status), _status);
                            if (_aisNavstatus != null)
                            {
                                _aisNavstatus = _aisNavstatus.Replace('_', ' ');
                            }
                        }
                    }
                    if (h2 == 129794)
                    {
                        int dataIndex = data[0];

                        if ((dataIndex >= 0 || dataIndex <= 224) && dataIndex % 32 == 0)
                        {
                            imoByte = data[7];
                        }

                        if ((dataIndex - 1) % 32 == 0 && dataIndex - 1 >= 0 && dataIndex - 1 <= 224)
                        {
                            byte[] byteArray1 = new byte[] { data[4], data[5], data[6], data[7] };
                            result = System.Text.Encoding.ASCII.GetString(byteArray1);
                            ais_IMO = Combine(imoByte, data[1], data[2], data[3]);
                        }

                        if ((dataIndex - 2) % 32 == 0 && dataIndex - 2 >= 0 && dataIndex - 2 <= 224)
                        {
                            byte[] byteArray2 = new byte[] { data[4], data[5], data[6], data[7] };
                            result1 = System.Text.Encoding.ASCII.GetString(byteArray2);
                            var singleByteArray = new byte[] { data[1], data[2] };
                            var aiscallSign = System.Text.Encoding.ASCII.GetString(singleByteArray);
                            _aiscallSign = (result + aiscallSign);
                        }

                        if ((dataIndex - 3) % 32 == 0 && dataIndex - 3 >= 0 && dataIndex - 3 <= 224)
                        {
                            byte[] byteArray3 = new byte[] { data[1], data[2], data[3], data[4], data[5], data[6], data[7] };
                            result2 = System.Text.Encoding.ASCII.GetString(byteArray3);
                        }

                        if ((dataIndex - 4) % 32 == 0 && dataIndex - 4 >= 0 && dataIndex - 4 <= 224)
                        {
                            byte[] byteArray4 = new byte[] { data[1], data[2], data[3], data[4], data[5], data[6], data[7] };
                            result3 = System.Text.Encoding.ASCII.GetString(byteArray4);
                        }

                        if ((dataIndex - 5) % 32 == 0 && dataIndex - 5 >= 0 && dataIndex - 5 <= 224)
                        {
                            byte[] byteArray5 = new byte[] { data[1], data[2] };
                            string result4 = System.Text.Encoding.ASCII.GetString(byteArray5);
                            _aisName = (result1 + result2 + result3 + result4);

                            int num = Convert.ToInt32(data[3]);
                            int num2 = num / 10;
                            AIS_Ship_Cargo_Type m_ShipCargoType;

                            if (Enum.IsDefined(typeof(AIS_Ship_Cargo_Type), num))
                            {
                                m_ShipCargoType = (AIS_Ship_Cargo_Type)num;
                            }
                            else if (Enum.IsDefined(typeof(AIS_Ship_Cargo_Type), num2))
                            {
                                m_ShipCargoType = (AIS_Ship_Cargo_Type)num2;
                            }
                            else
                            {
                                m_ShipCargoType = AIS_Ship_Cargo_Type.NOT_AVAILABLE;
                            }

                            // Get the enum name and replace underscores with spaces
                            cargoShipType = Enum.GetName(typeof(AIS_Ship_Cargo_Type), m_ShipCargoType);
                            if (cargoShipType != null)
                            {
                                cargoShipType = cargoShipType.Replace('_', ' ');
                            }

                            _shipLength = Mix(data[4], data[5]) / 10d;
                            _shipBeam = Mix(data[6], data[7]) / 10d;
                        }
                        if ((dataIndex - 7) % 32 == 0 && dataIndex - 7 >= 0 && dataIndex - 7 <= 224)
                        {
                            _shipDraft = Mix(data[4], data[5]) / 100d;
                        }
                    }
                }
            }
        }
    }
}
