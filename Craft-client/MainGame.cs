﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Craft_client.objects;

namespace Craft_client
{
    public partial class MainGame : Form
    {
        static HttpClient client;
        static long SessionId; // Current session id
        static string level_name; // level string (Desert, Sea, Swamp, Space)
        static long PlayerId; //current player id

        static int GameState = 0; // Game state = 0 (deployment) / 1 (started, but waiting for enemy deployment) / 2 (waiting for enemy move) / 3 (player turn)

        static Ship[] ships = new Ship[10]; // Unsent ships log
        Facade factories = new Facade();
        static int unsent_ships_count = 0; // count of ships already logged <- once 10 - initiate game

        //10x10 board of buttons a.k.a single game board
        Button[,] button_grid_player = new Button[10, 10];
        Button[,] button_grid_enemy = new Button[10, 10];

        public int[] Ship_Count = new int[4] { 4, 6, 6, 4 };
        // Log for last ships placed
        public int[] Last_Row = new int[100];
        public int[] Last_Collumn = new int[100];
        public int Last_step_count = 0;

        ShipPlacementController PlaceAircarrier = new ShipPlacementController(new AircarrierPlaced());
        ShipPlacementController PlaceWarship = new ShipPlacementController(new WarshipPlaced());
        ShipPlacementController PlaceSubmarine = new ShipPlacementController(new SubmarinePlaced());
        ShipPlacementController PlaceCruiser = new ShipPlacementController(new CruiserPlaced());

        GameActionsController Game = new GameActionsController(); // game actions methods

        public MainGame(HttpClient _client, long _sessionId, string _level_name, long _playerID)
        {
            InitializeComponent();
            populateGameBoard();

            client = _client;
            SessionId = _sessionId;
            level_name = _level_name;
            PlayerId = _playerID;
        }

        private void MainGame_Load(object sender, EventArgs e)
        {
            panel2.Enabled = false; // Enemy panel disabled \
            button1.Enabled = false; // block button         until ships are placed
            label4.Visible = true; // Label visible         /
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        /// <summary>
        ///  Populate panel 1 and Panel 2 with buttons a.k.a gameboard
        /// </summary>
        private void populateGameBoard()
        {
            int buttonSize = panel1.Width / 10;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    //initiate buttons and set their size
                    button_grid_enemy[i, j] = new Button();
                    button_grid_enemy[i, j].Height = buttonSize;
                    button_grid_enemy[i, j].Width = buttonSize;

                    button_grid_player[i, j] = new Button();
                    button_grid_player[i, j].Height = buttonSize;
                    button_grid_player[i, j].Width = buttonSize;

                    //physically add click events
                    button_grid_player[i, j].Click += Grid_Button_Click_Player;
                    button_grid_enemy[i, j].Click += Grid_Button_Click_Enemy;

                    //add buttons on the panel
                    panel1.Controls.Add(button_grid_player[i, j]);
                    panel2.Controls.Add(button_grid_enemy[i, j]);

                    //set button location on the panel
                    button_grid_player[i, j].Location = new Point(i * buttonSize, j * buttonSize);
                    button_grid_enemy[i, j].Location = new Point(i * buttonSize, j * buttonSize);

                    //set button.tag as id of a button with coordinates
                    button_grid_player[i, j].Tag = new Point(i, j);
                    button_grid_enemy[i, j].Tag = new Point(i, j);
                }
            }

        }

        /// <summary>
        /// Click events for each button on enemy panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_Button_Click_Enemy(object sender, EventArgs e)
        {
            //get the row and collumn
            Button clickedButton = (Button)sender;
            Point location = (Point)clickedButton.Tag;

            int row = location.X; //row
            int collumn = location.Y; //collumn

            //shoot
            panel2.Enabled = false;
            panel1.Enabled = false;
            //get information about the coordinate we shot at
        }

        /// <summary>
        /// click events for each button on player panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_Button_Click_Player(object sender, EventArgs e)
        {
            //get the row and collumn
            Button clicked_Button = (Button)sender; //cast button object
            Point location = (Point)clicked_Button.Tag; //Get button tag (we added coords to button tag)
            int row = location.X; //row
            int collumn = location.Y; //collumn

            //place ships while game state = 0 (deployment)
            if (GameState == 0)
            {
                Disable_Nearby_Buttons(row, collumn, clicked_Button); // log where and what ship was placed
            } else if (GameState == 3) // if player turn
            {
                //enable special skills
            }
            
        }

        /// <summary>
        /// Disable nearby buttons - ships can not touch
        /// Cruiser - 1 window (max 4 ships)
        /// Submarine - 2 windows (max 3 ships)
        /// Warship - 3 windows (max 2 ships)
        /// Aircarrier - 4 windows (max 1 ship)
        /// </summary>
        private void Disable_Nearby_Buttons(int row, int collumn, Button clicked_Button)
        {
            RadioButton radio_button = panel3.Controls.OfType<RadioButton>()
                                       .Where(x => x.Checked).FirstOrDefault(); // get checked radio button

            if (radio_button != null)
            {
                switch (radio_button.Text)
                    {
                        case "Cruiser":
                            Disable_Radio_Buttons(radio_button, 0);
                            button_grid_player = PlaceCruiser.ShipPlaced(row, collumn, button_grid_player, Ship_Count, Last_Row, Last_Collumn, Last_step_count);
                            clicked_Button.BackColor = Color.Aqua;
                        break;

                        case "Submarine":
                            Disable_Radio_Buttons(radio_button, 1);
                            button_grid_player = PlaceSubmarine.ShipPlaced(row, collumn, button_grid_player, Ship_Count, Last_Row, Last_Collumn, Last_step_count);
                            clicked_Button.BackColor = Color.Azure;
                            break;

                        case "Warship":
                            Disable_Radio_Buttons(radio_button, 2);
                            button_grid_player = PlaceWarship.ShipPlaced(row, collumn, button_grid_player, Ship_Count, Last_Row, Last_Collumn, Last_step_count);
                            clicked_Button.BackColor = Color.Bisque;
                            break;

                        case "Aircarrier":
                            Disable_Radio_Buttons(radio_button, 3);
                            button_grid_player = PlaceAircarrier.ShipPlaced(row, collumn, button_grid_player, Ship_Count, Last_Row, Last_Collumn, Last_step_count);
                            clicked_Button.BackColor = Color.BlueViolet;
                            break;
                    }

                Last_step_count++; //placement was made, coordinates of placement will be added to Last_row and Last_collumn
                Add_Ship_To_Log(row, collumn, radio_button.Text);
                Check_If_Ships_Created();
            }
            
        }
        /// <summary>
        /// Check if all chips were created 
        /// </summary>
        private void Check_If_Ships_Created()
        {
            if (unsent_ships_count == 10)
            {
                button1.Enabled = true; // enable start button
                label4.Text = "All ready! Press start:"; // Label visible
            }
        }
       
        /// <summary>
        /// Assign ship object for sending to server
        /// </summary>
        /// <param name="row"></param>
        /// <param name="collumn"></param>
        /// <param name="text"></param>
        private void Add_Ship_To_Log(int row, int collumn, string ship_type)
        {
            ships[unsent_ships_count] = factories.createShip(row, collumn, ship_type, level_name);
            Console.WriteLine(ships[unsent_ships_count].getShipType());
            unsent_ships_count++; //ship created
        }
             

        /// <summary>
        /// Disable radio buttons limiting number of ships available
        /// </summary>
        private void Disable_Radio_Buttons(RadioButton radio_button, int ship_number)
        {
            if(ship_number == 1)
            {
                Ship_Count[ship_number] = Ship_Count[ship_number] - 2;
            }
            else if(ship_number == 2)
            {
                Ship_Count[ship_number] = Ship_Count[ship_number] - 3;
            }
            else if(ship_number == 3)
            {
                Ship_Count[ship_number] = Ship_Count[ship_number] - 4;
            }
            else
            {
                Ship_Count[ship_number]--;
            }
            
            Console.WriteLine(Ship_Count[ship_number]);
            if (Ship_Count[ship_number] == 0)
            {
                radio_button.Enabled = false;
                radio_button.Checked = false;
            }
            label5.Text = Ship_Count[0].ToString();
            label6.Text = (Ship_Count[1] / 2).ToString();
            label7.Text = (Ship_Count[2] / 3).ToString();
            label8.Text = (Ship_Count[3] / 4).ToString();
        }

        /// <summary>
        /// After ship placement, start the battle - send ships log to server and wait for other player
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button1_Click(object sender, EventArgs e)
        {
            GameState = 1;

            button1.Text = "Loading";
            button1.Enabled = false; // enable start button
            label4.Text = "Waiting for other player"; // Label visible

            //send ships to server (to do).


            //check if another player is ready
            GameState = await Game.InitializeGame(client, SessionId, PlayerId);

            if (GameState == 2) // enemy turn
            {
                panel1.Enabled = false;

            } else if (GameState == 3) // player turn
            {
                panel2.Enabled = true;
            }
        }
        class Facade
        {
            private UnitFactory DesertFactory = new DesertShipFactory();
            private UnitFactory SeaFactory = new SeaShipFactory();
            private UnitFactory SpaceFactory = new SpaceShipFactory();
            private UnitFactory SwampFactory = new SwampShipFactory();

            public Ship createShip(int row, int collumn, string ship_type, string level_name)
            {
                Ship ship;
                switch (level_name)
                {
                    case "Desert":
                        switch (ship_type)
                        {
                            case "Cruiser":
                                if (DesertFactory.CruiserCount < 4)
                                {
                                    ship = DesertFactory.createCruiser(row, collumn, ship_type);
                                    DesertFactory.CruiserCount++;
                                    return ship;
                                }
                                break;

                            case "Submarine":
                                if (DesertFactory.SubmarineCount < 3)
                                {
                                    ship = DesertFactory.createSubmarine(row, collumn, ship_type);
                                    DesertFactory.SubmarineCount++;
                                    return ship;
                                }
                                break;

                            case "Warship":
                                if (DesertFactory.WarshipCount < 2)
                                {
                                    ship = DesertFactory.createWarship(row, collumn, ship_type);
                                    DesertFactory.WarshipCount++;
                                    return ship;
                                }
                                break;

                            case "Aircarrier":
                                if (DesertFactory.AircarrierCount < 1)
                                {
                                    ship = DesertFactory.createAircarrier(row, collumn, ship_type);
                                    DesertFactory.AircarrierCount++;
                                    return ship;
                                }
                                break;
                        }
                        break;
                    case "Sea":
                        switch (ship_type)
                        {
                            case "Cruiser":
                                if (SeaFactory.CruiserCount < 4)
                                {
                                    ship = SeaFactory.createCruiser(row, collumn, ship_type);
                                    SeaFactory.CruiserCount++;
                                    return ship;
                                }
                                break;

                            case "Submarine":
                                if (SeaFactory.SubmarineCount < 3)
                                {
                                    ship = SeaFactory.createSubmarine(row, collumn, ship_type);
                                    SeaFactory.SubmarineCount++;
                                    return ship;
                                }
                                break;

                            case "Warship":
                                if (SeaFactory.WarshipCount < 2)
                                {
                                    ship = SeaFactory.createWarship(row, collumn, ship_type);
                                    SeaFactory.WarshipCount++;
                                    return ship;
                                }
                                break;

                            case "Aircarrier":
                                if (SeaFactory.AircarrierCount < 1)
                                {
                                    ship = SeaFactory.createAircarrier(row, collumn, ship_type);
                                    SeaFactory.AircarrierCount++;
                                    return ship;
                                }
                                break;
                        }
                        break;
                    case "Space":
                        switch (ship_type)
                        {
                            case "Cruiser":
                                if (SpaceFactory.CruiserCount < 4)
                                {
                                    ship = SpaceFactory.createCruiser(row, collumn, ship_type);
                                    SpaceFactory.CruiserCount++;

                                }
                                break;

                            case "Submarine":
                                if (SpaceFactory.SubmarineCount < 3)
                                {
                                    ship = SpaceFactory.createSubmarine(row, collumn, ship_type);
                                    SpaceFactory.SubmarineCount++;
                                    return ship;
                                }
                                break;

                            case "Warship":
                                if (SpaceFactory.WarshipCount < 2)
                                {
                                    ship = SpaceFactory.createWarship(row, collumn, ship_type);
                                    SpaceFactory.WarshipCount++;
                                    return ship;
                                }
                                break;

                            case "Aircarrier":
                                if (SpaceFactory.AircarrierCount < 1)
                                {
                                    ship = SpaceFactory.createAircarrier(row, collumn, ship_type);
                                    SpaceFactory.AircarrierCount++;
                                    return ship;
                                }
                                break;
                        }
                        break;

                    case "Swamp":
                        switch (ship_type)
                        {
                            case "Cruiser":
                                if (SwampFactory.CruiserCount < 4)
                                {
                                    ship = SwampFactory.createCruiser(row, collumn, ship_type);
                                    SwampFactory.CruiserCount++;
                                    return ship;
                                }
                                break;

                            case "Submarine":
                                if (SwampFactory.SubmarineCount < 3)
                                {
                                    ship = SwampFactory.createSubmarine(row, collumn, ship_type);
                                    SwampFactory.SubmarineCount++;
                                    return ship;
                                }
                                break;

                            case "Warship":
                                if (SwampFactory.WarshipCount < 2)
                                {
                                    ship = SwampFactory.createWarship(row, collumn, ship_type);
                                    SwampFactory.WarshipCount++;
                                    return ship;
                                }
                                break;

                            case "Aircarrier":
                                if (SwampFactory.AircarrierCount < 1)
                                {
                                    ship = SwampFactory.createAircarrier(row, collumn, ship_type);
                                    SwampFactory.AircarrierCount++;
                                    return ship;
                                }
                                break;
                        }
                        break;
                }
                return null;
            }
            public int remainingShips(string level_name, string ship_type)
            {
                switch (level_name)
                {
                    case "Desert":
                        switch (ship_type)
                        {
                            case "Cruiser":
                                return DesertFactory.CruiserCount;
                            case "Submarine":
                                return DesertFactory.SubmarineCount;
                            case "Aircarrier":
                                return DesertFactory.AircarrierCount;
                            case "Warship":
                                return DesertFactory.WarshipCount;
                        }
                        break;
                    case "Sea":
                        switch (ship_type)
                        {
                            case "Cruiser":
                                return SeaFactory.CruiserCount;
                            case "Submarine":
                                return SeaFactory.SubmarineCount;
                            case "Aircarrier":
                                return SeaFactory.AircarrierCount;
                            case "Warship":
                                return SeaFactory.WarshipCount;
                        }
                        break;
                    case "Swamp":
                        switch (ship_type)
                        {
                            case "Cruiser":
                                return SwampFactory.CruiserCount;
                            case "Submarine":
                                return SwampFactory.SubmarineCount;
                            case "Aircarrier":
                                return SwampFactory.AircarrierCount;
                            case "Warship":
                                return SwampFactory.WarshipCount;
                        }
                        break;
                    case "Space":
                        switch (ship_type)
                        {
                            case "Cruiser":
                                return SpaceFactory.CruiserCount;
                            case "Submarine":
                                return SpaceFactory.SubmarineCount;
                            case "Aircarrier":
                                return SpaceFactory.AircarrierCount;
                            case "Warship":
                                return SpaceFactory.WarshipCount;
                        }
                        break;
                }
                return 0;
            }
        }
        abstract class UnitFactory
        {
            public int CruiserCount = 0;
            public int WarshipCount = 0;
            public int AircarrierCount = 0;
            public int SubmarineCount = 0;

            public abstract Ship createCruiser(int row, int collumn, string ship_type);
            public abstract Ship createWarship(int row, int collumn, string ship_type);
            public abstract Ship createAircarrier(int row, int collumn, string ship_type);

            public abstract Ship createSubmarine(int row, int collumn, string ship_type);
        }
        class DesertShipFactory : UnitFactory
        {

            public override Ship createCruiser(int row, int collumn, string ship_type)
            {
                return new DesertCruiser(row, collumn, ship_type);
            }
            public override Ship createWarship(int row, int collumn, string ship_type)
            {
                return new DesertWarship(row, collumn, ship_type);
            }
            public override Ship createAircarrier(int row, int collumn, string ship_type)
            {
                return new DesertAircarrier(row, collumn, ship_type);
            }
            public override Ship createSubmarine(int row, int collumn, string ship_type)
            {
                return new DesertSubmarine(row, collumn, ship_type);
            }
        }
        class SwampShipFactory : UnitFactory
        {

            public override Ship createCruiser(int row, int collumn, string ship_type)
            {
                return new SwampCruiser(row, collumn, ship_type);
            }
            public override Ship createWarship(int row, int collumn, string ship_type)
            {
                return new SwampWarship(row, collumn, ship_type);
            }
            public override Ship createAircarrier(int row, int collumn, string ship_type)
            {
                return new SwampAircarrier(row, collumn, ship_type);
            }
            public override Ship createSubmarine(int row, int collumn, string ship_type)
            {
                return new SwampSubmarine(row, collumn, ship_type);
            }
        }
        class SeaShipFactory : UnitFactory
        {

            public override Ship createCruiser(int row, int collumn, string ship_type)
            {
                return new SeaCruiser(row, collumn, ship_type);
            }
            public override Ship createWarship(int row, int collumn, string ship_type)
            {
                return new SeaWarship(row, collumn, ship_type);
            }
            public override Ship createAircarrier(int row, int collumn, string ship_type)
            {
                return new SeaAircarrier(row, collumn, ship_type);
            }
            public override Ship createSubmarine(int row, int collumn, string ship_type)
            {
                return new SeaSubmarine(row, collumn, ship_type);
            }
        }
        class SpaceShipFactory : UnitFactory
        {

            public override Ship createCruiser(int row, int collumn, string ship_type)
            {
                return new SpaceCruiser(row, collumn, ship_type);
            }
            public override Ship createWarship(int row, int collumn, string ship_type)
            {
                return new SpaceWarship(row, collumn, ship_type);
            }
            public override Ship createAircarrier(int row, int collumn, string ship_type)
            {
                return new SpaceAircarrier(row, collumn, ship_type);
            }
            public override Ship createSubmarine(int row, int collumn, string ship_type)
            {
                return new SpaceSubmarine(row, collumn, ship_type);
            }
        }       

        class DesertCruiser : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public DesertCruiser(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class DesertSubmarine : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public DesertSubmarine(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class DesertWarship : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public DesertWarship(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class DesertAircarrier : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public DesertAircarrier(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SwampCruiser : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SwampCruiser(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SwampSubmarine : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SwampSubmarine(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SwampWarship : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SwampWarship(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SwampAircarrier : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SwampAircarrier(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SeaCruiser : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SeaCruiser(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SeaSubmarine : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SeaSubmarine(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SeaWarship : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SeaWarship(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SeaAircarrier : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SeaAircarrier(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SpaceCruiser : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SpaceCruiser(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SpaceSubmarine : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SpaceSubmarine(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SpaceWarship : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SpaceWarship(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
        class SpaceAircarrier : Ship
        {
            int Row;
            int Collumn;
            string Ship_type;

            public SpaceAircarrier(int row, int collumn, string ship_type)
            {
                this.Row = row;
                this.Collumn = collumn;
                this.Ship_type = ship_type;
            }
            public override string getShipType()
            {

                return Ship_type;
            }

            public override int getShipRow()
            {
                return Row;
            }

            public override int getShipCollumn()
            {
                return Collumn;
            }

        }
    }
}
