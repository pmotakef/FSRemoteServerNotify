using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Net.Sockets;

namespace FSMapClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TCPClient mapClient = null;

        public string serverIPAddress { get; set; }
        public int serverPort { get; set; }

        private bool userPlanePositionInitialized = false;
        private List<Esri.ArcGISRuntime.Geometry.MapPoint> wPoints;
        private bool flightPlanLoaded = false;
        private string planeIcoUri;
        private bool showPlanePath;
        private bool planePathInitialized = false;
        private bool showFlightPlan = true;
        private bool flightPlanInitialized = false;
        private bool followPlane = true;

        /* -------------------------------------------------------------------------------------------------------------------------------
         * -------------------------------------------------------------------------------------------------------------------------------
         * --------------------------------------------- Window handlers -----------------------------------------------------------------
         * -------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------- */


        public MainWindow()
        {
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ClientId = "3KZ7ETwhVwgMtBGY";

            try
            {
                Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to initialize the ArcGIS Runtime with the client ID provided: " + ex.Message);
            }
            
            this.Width = (int)Properties.Settings.Default["winWidth"];
            this.Height = (int)Properties.Settings.Default["winHeight"];
            serverIPAddress = (string)Properties.Settings.Default["hostIPAddress"];
            serverPort = (int)Properties.Settings.Default["hostPortNum"];
            InitializeComponent();
            DataContext = this;
            wPoints = new List<Esri.ArcGISRuntime.Geometry.MapPoint>();
            planeIcoUri = "Images/arrow.png";
            showPlanePath = true;
            ResetAll();
        }

        private void ResetAll()
        {
            if(userPlanePositionInitialized)
            {
                var planeLayer = MyMap.Layers["UserAirplaneCoord"];
                MyMap.Layers.Remove(planeLayer);
                if (flightPlanLoaded)
                {
                    var flightPlanLayer = MyMap.Layers["UserPlaneWaypoints"];
                    MyMap.Layers.Remove(flightPlanLayer);
                }
            }
            userPlanePositionInitialized = false;
            flightPlanLoaded = false;
            wPoints.Clear();
            planePathInitialized = false;
            clearPlanePathLayer();
        }

        private void mnuFileExit_Click(object sender, RoutedEventArgs e)
        {
            if (mapClient != null)
            {
                mapClient.Stop();
                mapClient = null;
            }
            this.Close();
        }

        private void mnuUseLabels_Checked(object sender, RoutedEventArgs e)
        {
            drawLableLayer(true);
        }

        private void mnuUseLabels_Unchecked(object sender, RoutedEventArgs e)
        {
            drawLableLayer(false);
        }

        private void mnuResetPath_Click(object sender, RoutedEventArgs e)
        {
            clearPlanePathLayer();
        }



        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            MapClientSetup mcAbout = new MapClientSetup();
            mcAbout.Owner = this;
            mcAbout.Show();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Properties.Settings.Default["winWidth"] = (int)this.Width;
            Properties.Settings.Default["winHeight"] = (int)this.Height;
            Properties.Settings.Default.Save();
        }

        private void mnuFollowPlane_Checked(object sender, RoutedEventArgs e)
        {
            followPlane = true;
        }

        private void mnuFollowPlane_Unchecked(object sender, RoutedEventArgs e)
        {
            followPlane = false;
        }

        private void mnuShowPlanePath_Checked(object sender, RoutedEventArgs e)
        {
            showPlanePath = true;
        }

        private void mnuShowPlanePath_Unchecked(object sender, RoutedEventArgs e)
        {
            showPlanePath = false;
            clearPlanePathLayer();
        }

        private void mnuShowFlightPlan_Checked(object sender, RoutedEventArgs e)
        {
            showFlightPlan = true;
            if (flightPlanLoaded)
            {
                drawUserPlaneWaypoint();
            }
        }

        private void mnuShowFlightPlan_Unchecked(object sender, RoutedEventArgs e)
        {
            showFlightPlan = false;
            clearFlightPlanLayer();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mapClient != null)
            {
                mapClient.Stop();
                mapClient = null;
            }
        }

        /* -------------------------------------------------------------------------------------------------------------------------------
         * -------------------------------------------------------------------------------------------------------------------------------
         * --------------------------------------------- Client handlers -----------------------------------------------------------------
         * -------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------- */

        private void mnuConnect_Click(object sender, RoutedEventArgs e)
        {
            if (mapClient == null)
            {
                
                try
                {
                    IPAddress adr = IPAddress.Parse(serverIPAddress);
                }
                catch (FormatException)
                {
                    MessageBox.Show("Invalid IP Address format!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                catch (ArgumentNullException)
                {
                    MessageBox.Show("IP Address can't be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                mapClient = new TCPClient(IPAddress.Parse(serverIPAddress), serverPort, 0);
                mapClient.ServerConnectionEstablished += new ConnectionEventHandler(serverConnectionStatus);
                mapClient.ServerDataTransmissionRecieved += new DataRecieved(ServerDataReceieved);
                mapClient.OnError += new ErrorHandler(onClientError);
                try
                {
                    mapClient.ConnectToServer(true);
                }
                catch
                {
                    MessageBox.Show("Couldn't find the server");
                    return;
                }
            }
            else
            {
                mapClient.Stop();
                mapClient = null;
            }
        }

        private void onClientError(object sender, ErrorEventArgs e)
        {
            if(e.GetException() is SocketException)
            {
                MessageBox.Show("Couldn't find the server!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ServerDataReceieved(object sender, byte[] buffer)
        {
            switch (buffer[0])
            {
                case (byte)FSDataType.DT_MAP:
                    double latitude = BitConverter.ToDouble(buffer, 1);
                    double longitude = BitConverter.ToDouble(buffer, (sizeof(double)) + 1);
                    double heading = BitConverter.ToDouble(buffer, (sizeof(double) * 2) + 1);

                    if (userPlanePositionInitialized)
                    {
                        updateUserPlaneCoord(longitude, latitude, (heading * 180.0 / Math.PI));
                    }
                    else
                    {
                        setUserPlaneCoord(longitude, latitude, (heading * 180.0 / Math.PI));
                        userPlanePositionInitialized = true;
                    }

                    if(planePathInitialized)
                    {
                        updatePlanePathLayer(longitude, latitude);
                    }
                    else
                    {
                        initPlanePathLayer(longitude, latitude);
                    }
                    if (followPlane)
                    {
                        centerAtPoint(longitude, latitude);
                    }
                    break;

                case (byte)FSDataType.DT_FLIGHTPLAN:
                    if (flightPlanLoaded && buffer[1] > 1)
                    {
                        wPoints.Clear();
                        flightPlanLoaded = false;
                    }
                    double lt = BitConverter.ToDouble(buffer, 2);
                    double ln = BitConverter.ToDouble(buffer, (sizeof(double)) + 2);
                    wPoints.Add(new Esri.ArcGISRuntime.Geometry.MapPoint(ln, lt, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84));

                    if (buffer[1] < 1)
                    {
                        if (showFlightPlan)
                        {
                            drawUserPlaneWaypoint();
                        }
                        flightPlanLoaded = true;
                    }

                    break;

                case (byte)FSDataType.DT_RESTART:
                    ResetAll();
                    break;

                default:
                    break;


            }
        }

        private void serverConnectionStatus(object sender, bool state)
        {
            if (state)
            {
                mnuConnectStat.Header = "Disconnect";
                userPlanePositionInitialized = false;

                CLIENT_TYPE mapInfoClient = new CLIENT_TYPE();
                mapInfoClient.ccode = ClientCode.CC_MAP_INFO;
                mapInfoClient.ReleaseVersion = 1;
                mapInfoClient.MajorVersion = 0;
                mapInfoClient.MinorVersion = 0;

                List<byte[]> sndData = DataManager.convertDataStructToByte(FSDataType.DT_CLIENTTYPE, mapInfoClient).ToList();
                if (sndData.Count > 0)
                {
                    mapClient.sendData(sndData[0]);
                }
            }
            else
            {
                mnuConnectStat.Header = "Connect";
                ResetAll();
                if (mapClient != null)
                {
                    mapClient.Stop();
                    mapClient = null;
                }
            }
        }

        private void mnuSetPortIP_Click(object sender, RoutedEventArgs e)
        {
            serverIPAddress = mnuIPAdr.Text;
            serverPort = Convert.ToInt32(mnuPortNum.Text);

            Properties.Settings.Default["hostPortNum"] = serverPort;
            Properties.Settings.Default["hostIPAddress"] = serverIPAddress;
            Properties.Settings.Default.Save();
        }

        /* -------------------------------------------------------------------------------------------------------------------------------
         * -------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------- Map drawing -----------------------------------------------------------------
         * -------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------- */

        private void cmbMapType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            var sel = combo.SelectedItem as ComboBoxItem;
            if (sel.Tag == null) { return; }
            // Find and remove the current basemap layer from the map
            if (MyMap == null) { return; }
            var oldBasemap = MyMap.Layers["BaseMap"];
            MyMap.Layers.Remove(oldBasemap);
            // Create a new basemap layer
            var newBasemap = new Esri.ArcGISRuntime.Layers.ArcGISTiledMapServiceLayer();
            // Set the ServiceUri with the url defined for the ComboBoxItem's Tag
            newBasemap.ServiceUri = sel.Tag.ToString();
            // Give the layer the same ID so it can still be found with the code above
            newBasemap.ID = "BaseMap";
            // Insert the new basemap layer as the first (bottom) layer in the map
            MyMap.Layers.Insert(0, newBasemap);

        }

        private void drawLableLayer(bool enable)
        {
            var secondLayer = MyMap.Layers["SecondLayer"];
            if (secondLayer != null && enable == false)
            {
                MyMap.Layers.Remove(secondLayer);
            }
            else if (secondLayer == null && enable == true)
            {
                var newSecLayer = new Esri.ArcGISRuntime.Layers.ArcGISTiledMapServiceLayer();
                newSecLayer.ID = "SecondLayer";
                newSecLayer.ServiceUri = "http://services.arcgisonline.com/ArcGIS/rest/services/Reference/World_Boundaries_and_Places/MapServer";
                MyMap.Layers.Insert(1, newSecLayer);
            }
        }

        private void updateUserPlaneCoord(double coordx, double coordy, double rotation)
        {
            var userPlaneGraphicsLayer = MyMap.Layers["UserAirplaneCoord"] as Esri.ArcGISRuntime.Layers.GraphicsLayer;

            foreach (var g in userPlaneGraphicsLayer.Graphics)
            {
                var rot = g.Symbol as Esri.ArcGISRuntime.Symbology.PictureMarkerSymbol;
                if(rot != null)
                {
                    rot.Angle = rotation;
                }
                var point = g.Geometry as Esri.ArcGISRuntime.Geometry.MapPoint;
                if(point != null)
                {
                    g.Geometry = point.MoveTo(coordx, coordy);
                }
            }


        }

        private void updatePlanePathLayer(double coordx, double coordy)
        {
            if(!showPlanePath)
            {
                return;
            }

            var userPlanePathLayer = MyMap.Layers["UserPlaneRealPath"] as Esri.ArcGISRuntime.Layers.GraphicsLayer;

            foreach (var g in userPlanePathLayer.Graphics)
            {
                var lst = g.Geometry as Esri.ArcGISRuntime.Geometry.Polyline;
                if (lst != null)
                {
                    var polyBuilder = new Esri.ArcGISRuntime.Geometry.PolylineBuilder(lst);
                    polyBuilder.AddPoint(coordx, coordy);
                    g.Geometry = polyBuilder.ToGeometry();
                }
            }
        }

        private void setUserPlaneCoord(double coordx, double coordy, double rotation)
        {

            // --------------------------- User Plane live coordiantes and heading ---------------------
            var userPlaneGraphicsLayer = MyMap.Layers["UserAirplaneCoord"] as Esri.ArcGISRuntime.Layers.GraphicsLayer;
            if (userPlaneGraphicsLayer == null)
            {
                userPlaneGraphicsLayer = new Esri.ArcGISRuntime.Layers.GraphicsLayer();
                userPlaneGraphicsLayer.ID = "UserAirplaneCoord";
                userPlaneGraphicsLayer.RenderingMode = Esri.ArcGISRuntime.Layers.GraphicsRenderingMode.Dynamic;
                MyMap.Layers.Add(userPlaneGraphicsLayer);
            }

            var pictureSym = new Esri.ArcGISRuntime.Symbology.PictureMarkerSymbol();

            var path = System.IO.Path.Combine(Environment.CurrentDirectory, planeIcoUri);
            var uri = new Uri(path);
            pictureSym.SetSourceAsync(uri);
            pictureSym.Angle = rotation;
            pictureSym.Height = 50;
            pictureSym.Width = 50;


            var pointGraphic = new Esri.ArcGISRuntime.Layers.Graphic();
            var mapPoint = new Esri.ArcGISRuntime.Geometry.MapPoint(coordx, coordy, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84);
            pointGraphic.Geometry = mapPoint;
            pointGraphic.Symbol = pictureSym;
            userPlaneGraphicsLayer.Graphics.Add(pointGraphic);
        }

/*        private void AddMarker_Click(object sender, RoutedEventArgs e)
        {
            //setUserPlaneCoord(36.4, 51.2, 30.0);
            centerAtPoint(10.0, 10.0);
        }
  */     
        private void drawUserPlaneWaypoint()
        {
            var userPlaneWPLayer = MyMap.Layers["UserPlaneWaypoints"] as Esri.ArcGISRuntime.Layers.GraphicsLayer;
            if (userPlaneWPLayer == null)
            {
                userPlaneWPLayer = new Esri.ArcGISRuntime.Layers.GraphicsLayer();
                userPlaneWPLayer.ID = "UserPlaneWaypoints";
                userPlaneWPLayer.RenderingMode = Esri.ArcGISRuntime.Layers.GraphicsRenderingMode.Static;
                MyMap.Layers.Add(userPlaneWPLayer);
            }

            var lineSym = new Esri.ArcGISRuntime.Symbology.SimpleLineSymbol();
            lineSym.Color = Colors.Purple;
            lineSym.Width = 3;
            lineSym.Style = Esri.ArcGISRuntime.Symbology.SimpleLineStyle.Solid;

            Esri.ArcGISRuntime.Geometry.Polyline line = new Esri.ArcGISRuntime.Geometry.Polyline(wPoints);
            var lineGraphic = new Esri.ArcGISRuntime.Layers.Graphic();
            lineGraphic.Geometry = line;
            lineGraphic.Symbol = lineSym;
            userPlaneWPLayer.Graphics.Add(lineGraphic);

            flightPlanInitialized = true;
        }

        private void clearPlanePathLayer()
        {
            var planePathLayer = MyMap.Layers["UserPlaneRealPath"];
            if (planePathLayer != null)
            {
                MyMap.Layers.Remove(planePathLayer);
            }
            planePathInitialized = false;
        }

        private void initPlanePathLayer(double coordx, double coordy)
        {
            if (!showPlanePath)
            {
                return;
            }

            //var planePathLayer = MyMap.Layers["UserPlaneRealPath"] as Esri.ArcGISRuntime.Layers.GraphicsLayer;
            var planePathLayer = new Esri.ArcGISRuntime.Layers.GraphicsLayer();
            planePathLayer.ID = "UserPlaneRealPath";
            planePathLayer.RenderingMode = Esri.ArcGISRuntime.Layers.GraphicsRenderingMode.Dynamic;
            MyMap.Layers.Add(planePathLayer);

            var lineSym = new Esri.ArcGISRuntime.Symbology.SimpleLineSymbol();
            lineSym.Color = Colors.Red;
            lineSym.Width = 2;
            lineSym.Style = Esri.ArcGISRuntime.Symbology.SimpleLineStyle.Solid;

            List<Esri.ArcGISRuntime.Geometry.MapPoint> userPlanePositionsList = new List<Esri.ArcGISRuntime.Geometry.MapPoint>();
            userPlanePositionsList.Add(new Esri.ArcGISRuntime.Geometry.MapPoint(coordx, coordy, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84));
            userPlanePositionsList.Add(new Esri.ArcGISRuntime.Geometry.MapPoint(coordx, coordy, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84));


            Esri.ArcGISRuntime.Geometry.Polyline line = new Esri.ArcGISRuntime.Geometry.Polyline(userPlanePositionsList);
            var lineGraphic = new Esri.ArcGISRuntime.Layers.Graphic();
            lineGraphic.Geometry = line;
            lineGraphic.Symbol = lineSym;
            planePathLayer.Graphics.Add(lineGraphic);

            planePathInitialized = true;

        }

        private void cmbUserPlane_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            var sel = combo.SelectedItem as ComboBoxItem;
            if (sel.Tag == null) { return; }
            planeIcoUri = sel.Tag.ToString();
            updatePlaneIcon();
        }

        private void updatePlaneIcon()
        {
            if (mapClient == null)
                return;

            var userPlaneGraphicsLayer = MyMap.Layers["UserAirplaneCoord"] as Esri.ArcGISRuntime.Layers.GraphicsLayer;
            if (userPlaneGraphicsLayer == null)
                return;

            foreach (var g in userPlaneGraphicsLayer.Graphics)
            {
                var ico = g.Symbol as Esri.ArcGISRuntime.Symbology.PictureMarkerSymbol;
                if (ico != null)
                {
                    var path = System.IO.Path.Combine(Environment.CurrentDirectory, planeIcoUri);
                    var uri = new Uri(path);
                    ico.SetSourceAsync(uri);
                }
            }

        }


        private void clearFlightPlanLayer()
        {
            if (!flightPlanInitialized)
            {
                return;
            }
            var flightPlanLayer = MyMap.Layers["UserPlaneWaypoints"];
            MyMap.Layers.Remove(flightPlanLayer);
            flightPlanInitialized = false;
        }

        private async void centerAtPoint(double coordx, double coordy)
        {
            Esri.ArcGISRuntime.Geometry.MapPoint mp = new Esri.ArcGISRuntime.Geometry.MapPoint(coordx, coordy, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84);
            await MyMapView.SetViewAsync(mp);
        }


        /*
       private void drawCloudDensity (byte[] buffer, double x1, double y1, double x2, double y2)
       {
           var cloudGraphicsLayer = MyMap.Layers["CloudDensityLayer"] as Esri.ArcGISRuntime.Layers.GraphicsLayer;
           if (cloudGraphicsLayer == null)
           {
               cloudGraphicsLayer = new Esri.ArcGISRuntime.Layers.GraphicsLayer();
               cloudGraphicsLayer.ID = "CloudDensityLayer";
               cloudGraphicsLayer.RenderingMode = Esri.ArcGISRuntime.Layers.GraphicsRenderingMode.Dynamic;
               MyMap.Layers.Add(cloudGraphicsLayer);
           }
           var polySym = new Esri.ArcGISRuntime.Symbology.PictureFillSymbol();
           var polysim2 = new Esri.ArcGISRuntime.Symbology.SimpleFillSymbol();
           polysim2.Color = Colors.AliceBlue;
           polysim2.Style = Esri.ArcGISRuntime.Symbology.SimpleFillStyle.Solid;

           Stream cloudStream = new MemoryStream(buffer);

           polySym.SetSourceAsync(cloudStream);
           List<Esri.ArcGISRuntime.Geometry.MapPoint> coordList = new List<Esri.ArcGISRuntime.Geometry.MapPoint>();
           coordList.Add(new Esri.ArcGISRuntime.Geometry.MapPoint(x1, y1, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84));
           coordList.Add(new Esri.ArcGISRuntime.Geometry.MapPoint(x2, y1, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84));
           coordList.Add(new Esri.ArcGISRuntime.Geometry.MapPoint(x2, y2, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84));
           coordList.Add(new Esri.ArcGISRuntime.Geometry.MapPoint(x1, y2, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84));

           var pointGraphic = new Esri.ArcGISRuntime.Layers.Graphic();
           Esri.ArcGISRuntime.Geometry.Polygon poly = new Esri.ArcGISRuntime.Geometry.Polygon(coordList);
           pointGraphic.Geometry = poly;
           pointGraphic.Symbol = polySym;
           cloudGraphicsLayer.Graphics.Add(pointGraphic);
       }
       */
    }
}
