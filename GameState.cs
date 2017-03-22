//-----------------------------------------------------------------------------
// The main GameState Singleton. All actions that change the game state,
// as well as any global updates that happen during gameplay occur in here.
// Because of this, the file is relatively lengthy.
//
// __Defense Sample for Game Programming Algorithms and Techniques
// Copyright (C) Sanjay Madhav. All rights reserved.
//
// Released under the Microsoft Permissive License.
// See LICENSE.txt for full details.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace itp380
{
    public enum eGameState
    {
        None = 0,
        MainMenu,
        Gameplay,
    }

    public class GameState : itp380.Patterns.Singleton<GameState>
    {
        Game m_Game;
        public List<Objects.Enemy> m_enemies = new List<Objects.Enemy>();
        public List<Objects.Item> m_items = new List<Objects.Item>();
        public List<Objects.Texture_Block> m_blocks = new List<Objects.Texture_Block>();
        public List<Objects.Platform> m_platforms = new List<Objects.Platform>();
        eGameState m_State;
        const int texture_size = 10;
        public const int TEXTURE_GRASS = 0;
        public const int TEXTURE_WOOD = 1;
        public const int TEXTURE_RED_CARPET = 2;
        public const int TEXTURE_RED_CARPET_SIDE = 3;
        public const int TEXTURE_WINDOW = 4;
        public Texture2D[] m_textures;
        public Random m_Random;
        public eGameState State
        {
            get { return m_State; }
        }
        public const String MODEL_BLOCK = "Environment/block";
        public const String MODEL_FLOOR = "Environment/floor";
        public const String MODEL_PLATFORM = "Environment/platform";

        
        eGameState m_NextState;
        Stack<UI.UIScreen> m_UIStack;
        bool m_bPaused = false;
        
        BasicEffect effect;
        public static int world_width = 90;
        public static int world_length = 70;
        public static int world_height = 10;
        public static float blockscale = 2.5f;

       // public Objects.Texture_Block[, ,] m_tiles = new Objects.Texture_Block[world_width, world_height, world_length];
        //public enum mesh_type
        //{

        //}
        public int[, ,] m_tiles = new int[world_width, world_height, world_length];
        public int CURRENT_LEVEL = 1;
        public static int crystal_count = 0;
        public float DESTINATION_X_MIN = 0;
        public float DESTINATION_Z_MIN = 0;
        public float DESTINATION_X_MAX = 0;
        public float DESTINATION_Z_MAX = 0;
        public bool in_zone=false;
        public static bool souls_obtained = false;
        public bool IsPaused
        {
            get { return m_bPaused; }
            set { m_bPaused = value; }
        }

        public Objects.Boo boo;
        public List<Objects.Bomb_Enemy> bombemies;
        public float booYOffSet = .75f;
        //59 * blockscale, 0, 20 * blockscale
        //22 * blockscale, 0, 17 * blockscale
        //50f, 0, 120f
        //30f*blockscale, 0, 63*blockscale
        public Vector3 boospawn = new Vector3(40 * blockscale, 0f, 5 * blockscale);

        //FOR PARTICLES
        int explosionCounter = 0;
        int bloodCounter = 0;
        List<Particles.ParticleEmitter> particleEmitters = new List<Particles.ParticleEmitter>();


        //FOR BUILDING LEVELS
        public int[, ,] levelArray = new int[world_width, world_height, world_length];


        // Keeps track of all active game objects
        LinkedList<GameObject> m_GameObjects = new LinkedList<GameObject>();
        //LinkedList<Objects.Floor> m_tiles = new LinkedList<Objects.Floor>();
        // Objects.Floor tiles[world_size_width][world_size_length];
        // Camera Information
        Camera m_Camera;
        public Camera Camera
        {
            get { return m_Camera; }
        }

        public Matrix CameraMatrix
        {
            get { return m_Camera.CameraMatrix; }
        }

        // Timer class for the global GameState
        Utils.Timer m_Timer = new Utils.Timer();

        UI.UIGameplay m_UIGameplay;

        public void Start(Game game)
        {

            m_Game = game;
            m_State = eGameState.None;
            m_UIStack = new Stack<UI.UIScreen>();
            m_Camera = new Camera(m_Game);
            m_textures = new Texture2D[]{
                m_Game.Content.Load<Texture2D>("Environment/dead_grass"),
                m_Game.Content.Load<Texture2D>("Environment/dark_wood"),
                 m_Game.Content.Load<Texture2D>("Environment/red_carpet"),
                 m_Game.Content.Load<Texture2D>("Environment/red_carpet_side"),
                 m_Game.Content.Load<Texture2D>("Environment/Window")
            };
            m_Random = new Random();

        }

        public void SetState(eGameState NewState)
        {
            m_NextState = NewState;
        }

        private void HandleStateChange()
        {
            if (m_NextState == m_State)
                return;

            switch (m_NextState)
            {
                case eGameState.MainMenu:
                    m_UIStack.Clear();
                    m_UIGameplay = null;
                    m_Timer.RemoveAll();
                    m_UIStack.Push(new UI.UIMainMenu(m_Game.Content));
                    ClearGameObjects();
                    break;
                case eGameState.Gameplay:
                    SetupGameplay();
                    break;
            }

            m_State = m_NextState;
        }

        protected void ClearGameObjects()
        {
            // Clear out any and all game objects
            foreach (GameObject o in m_GameObjects)
            {
                RemoveGameObject(o, false);
            }
            m_GameObjects.Clear();
        }

        public void SetupGameplay()
        {
            crystal_count = 0;
            ClearGameObjects();
            m_UIStack.Clear();
            m_UIGameplay = new UI.UIGameplay(m_Game.Content);
            m_UIStack.Push(m_UIGameplay);

            m_bPaused = false;
            GraphicsManager.Get().ResetProjection();

            m_Timer.RemoveAll();

            bombemies = new List<Objects.Bomb_Enemy>();
           
            boo = new Objects.Boo(m_Game);
            
            spawnLevelOne();
            
            SpawnGameObject(boo);
            boo.spawnPos = boospawn + new Vector3(0, 0, 0);
            boo.spawnVel = Vector3.Zero;
            boo.spawnAng = MathHelper.Pi;
            boo.spawn();
            boo.RebuildWorldTransform();
            boo.place = new Vector3((int)((float)(boo.Position.X / blockscale)), ((boo.Position.Y + 1.25f) / blockscale + 1), (int)(boo.Position.Z / (float)blockscale));
            
            m_Camera.setTarget(boo);
            crystal_count = 0;

            XMLParser.Get().parseXML(m_tiles);

        }

        #region Build Levels

        public void spawnLevelOne()
        {
            Objects.Platform platform = new Objects.Platform(m_Game, 3f, 1, 2f, 0f, 3f, 0, 0f, 5f, 0);
            platform.setPosition(new Vector3(0,1f,10f));
           // SpawnGameObject(platform);
            //m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 2, 1);
            platform.setPosition(boospawn + new Vector3(0, 2f, 1f));
            //SpawnGameObject(platform);
           // m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, 2F, 1, 5f, 0f, 0f, 0f, 0f, 0f, 0f);
            platform.setPosition(new Vector3(17 * blockscale, 0, 27 * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, 2F, 1, -5f, 0f, 5f, 0f, 0f, 7, 0f);
            platform.setPosition(new Vector3(17 * blockscale, 0, 31 * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 2, 0, 0, 2, 0, 0, 5);
            platform.setPosition(new Vector3(20 * blockscale, 1f, 22 * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform", 1, 3, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(19 * blockscale, 7, 31 * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform", 2, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(24 * blockscale, 0, 35.5f * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform", 1, 4f, 0, 0, 7, 0, 0, 5);
            platform.setPosition(new Vector3(21 * blockscale, 6, 28 * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, 2F, 1, 2f, 0f, 0, 0f, 0f, 0, 0f);
            platform.setPosition(new Vector3(24 * blockscale, 4, 33 * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, 2F, 1, 2f, 0f, 5, 0f, 0f, 5*blockscale, 0f);
            platform.setPosition(new Vector3(53 * blockscale, 1f, 33 * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform", 1, 4f, 3, 0, 0, 5f, 0, 0);
            platform.setPosition(new Vector3(51 * blockscale, 4*blockscale, 35 * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale, 2f, 47f * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale + 6, 3f, 47f * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale + 12, 3f, 47f * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale + 18, 3f, 47f * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale + 22, 3f, 47f * blockscale + 2));
            SpawnGameObject(platform);
            m_platforms.Add(platform);
            platform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi/2);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale + 22, 3f, 47f * blockscale + 8));
            SpawnGameObject(platform);
            m_platforms.Add(platform);
            platform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi / 2);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale + 22, 3f, 47f * blockscale + 14));
            SpawnGameObject(platform);
            m_platforms.Add(platform);
            platform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi / 2);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale + 20, 3f, 47f * blockscale + 17));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale + 14, 3f, 47f * blockscale + 17));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform_rectangle", 1, 1, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale + 8, 3f, 47f * blockscale + 17));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, 2F, 1, 15f, 0f, 2, 0f, 0f, 5, 0f);
            platform.setPosition(new Vector3(23f * blockscale + 3, 3f, 47f * blockscale + 13));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, 2F, 1, 2, 0f, 0, 4f, 0f, 0, 4);
            platform.setPosition(new Vector3(23f * blockscale + 3, 8f, 49f * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, 2F, 1, 2, 4, 0, 0, 4, 0, 0);
            platform.setPosition(new Vector3(23f * blockscale - 1, 8f, 48f * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);

            platform = new Objects.Platform(m_Game, "Environment/platform", 2, 2, 0, 0, 0, 0, 0, 0);
            platform.setPosition(new Vector3(20f * blockscale - 2, 8f, 47f * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);
            


            ////spawn the enemies
            //Objects.Bomb_Enemy bombemy = new Objects.Bomb_Enemy(m_Game);
            //bombemy.setPosition(new Vector3(0, 0, 10) + boospawn);
            //Spawn_Enemy(bombemy);
            //bombemy = new Objects.Bomb_Enemy(m_Game);
            //bombemy.setPosition(new Vector3(0, 0, -10) + boospawn);
            //Spawn_Enemy(bombemy);

            //Objects.Haunter_Enemy haunter = new Objects.Haunter_Enemy(m_Game);
            //haunter.setPosition(new Vector3(40f, 0, 120f));
            //Spawn_Enemy(haunter);

            //haunter = new Objects.Haunter_Enemy(m_Game);
            //haunter.setPosition(new Vector3(70f, 0, 120f));
            //Spawn_Enemy(haunter);


            //right hallway of thwomps
            for (int i = 6; i >= 0; i--)
            {
                Objects.Thwomp_Enemy t_enemy = new Objects.Thwomp_Enemy(m_Game, (float)3f-i * .3f , true);
                t_enemy.setPosition(new Vector3(8+blockscale, 5, 25 * blockscale + i * t_enemy.Scale * 10));
                Objects.Thwomp_Enemy t2_enemy = new Objects.Thwomp_Enemy(m_Game, (float)3f-i * .3f , true);
                t2_enemy.setPosition(new Vector3(8-blockscale, 5, 25 * blockscale + i * t_enemy.Scale * 10));
                Spawn_Enemy(t_enemy);
                Spawn_Enemy(t2_enemy);
            }
            //right hallway of cannons
            for (int i = 1; i < 5; i++)
            {
                Objects.Cannon_Enemy c_enemy = new Objects.Cannon_Enemy(m_Game,i);
                if(i==1)c_enemy.Position = new Vector3( i*blockscale*5-2, -1, 16 * blockscale);
                else c_enemy.Position = new Vector3(i * blockscale * 5, -1, 16 * blockscale);
                c_enemy.stationary = true;
                c_enemy.rotateBackwards();
                Spawn_Enemy(c_enemy);
            }
            //Objects.Cannon_Enemy c1_enemy = new Objects.Cannon_Enemy(m_Game, 1);
            //c1_enemy.Position = new Vector3( blockscale * 5, -1, 16 * blockscale);
            //c1_enemy.stationary = true;
            //c1_enemy.rotateBackwards();
            //Spawn_Enemy(c1_enemy);
            Objects.Cannon_Enemy c2_enemy = new Objects.Cannon_Enemy(m_Game, 1);
            c2_enemy = new Objects.Cannon_Enemy(m_Game, 1);
            c2_enemy.Position = new Vector3(blockscale * 5-6, -1, 16 * blockscale);
            c2_enemy.stationary = true;
            c2_enemy.rotateBackwards();
            Spawn_Enemy(c2_enemy);
            c2_enemy = new Objects.Cannon_Enemy(m_Game, 1);
            c2_enemy.Position = new Vector3(blockscale * 2, -1, 64 * blockscale);
            c2_enemy.stationary = true;
            
            Spawn_Enemy(c2_enemy);
            c2_enemy = new Objects.Cannon_Enemy(m_Game, 1);
            c2_enemy.Position = new Vector3(blockscale * 4, -1, 64*blockscale );
            c2_enemy.stationary = true;
            
            Spawn_Enemy(c2_enemy);
            //Right Room
            
            Objects.Soul_Crystal soul_crystal = new Objects.Soul_Crystal(m_Game);
            soul_crystal.setPosition(new Vector3(24 * blockscale, .5f, 35.5f * blockscale));
            Spawn_Item(soul_crystal);

            soul_crystal = new Objects.Soul_Crystal(m_Game);
            soul_crystal.setPosition(new Vector3(20f * blockscale, 10f, 47f * blockscale));
            Spawn_Item(soul_crystal);

            /* FOR PARTICLES THAT FOLLOW ENEMIES */
            Particles.FireParticleSystem fire;
            Particles.ParticleEmitter bombFire;


           //left hallway
            Objects.Bomb_Enemy bomb_enemy = new Objects.Bomb_Enemy(m_Game,.95F);
            bomb_enemy.setPosition(new Vector3(7*blockscale, 0, 62*blockscale));
            Spawn_Enemy(bomb_enemy);
            
            fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            m_Game.Components.Add(fire);
            particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,1F);
            bomb_enemy.setPosition(new Vector3(7 * blockscale, 0, 62 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.91F);
            bomb_enemy.setPosition(new Vector3(9 * blockscale, 0, 62.5f * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.94F);
            bomb_enemy.setPosition(new Vector3(16 * blockscale, 0, 63 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game, .93F);
            bomb_enemy.setPosition(new Vector3(16 * blockscale, 0, 62 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.95F);
            bomb_enemy.setPosition(new Vector3(21 * blockscale, 0, 62.5f * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.98F);
            bomb_enemy.setPosition(new Vector3(22 * blockscale, 0, 63 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.99F);
            bomb_enemy.setPosition(new Vector3(25 * blockscale, 0, 62 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.93F);
            bomb_enemy.setPosition(new Vector3(30 * blockscale, 0, 61.8f * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.97F);
            bomb_enemy.setPosition(new Vector3(34 * blockscale, 0, 62.5f * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.98F);
            bomb_enemy.setPosition(new Vector3(35 * blockscale, 0, 63 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,1.01F);
            bomb_enemy.setPosition(new Vector3(40 * blockscale, 0, 62 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,1.02F);
            bomb_enemy.setPosition(new Vector3(44 * blockscale, 0, 62.5f * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,1.03F);
            bomb_enemy.setPosition(new Vector3(45 * blockscale, 0, 63 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.98F);
            bomb_enemy.setPosition(new Vector3(47 * blockscale, 0, 62 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.985F);
            bomb_enemy.setPosition(new Vector3(48 * blockscale, 0, 61.8f * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.987F);
            bomb_enemy.setPosition(new Vector3(50 * blockscale, 0, 62 * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            bomb_enemy = new Objects.Bomb_Enemy(m_Game,.988F);
            bomb_enemy.setPosition(new Vector3(51 * blockscale, 0, 61.8f * blockscale));
            Spawn_Enemy(bomb_enemy);
            //fire = new Particles.FireParticleSystem(m_Game, m_Game.Content);
            //bombFire = new Particles.ParticleEmitter(fire, 1000, bomb_enemy);
            //m_Game.Components.Add(fire);
            //particleEmitters.Add(bombFire);

            //left room after hallway
            for (int i = 0; i < 5; i++)
            {
                Objects.Cannon_Enemy c_enemy = new Objects.Cannon_Enemy(m_Game, 0.5f);
                c_enemy.Position = new Vector3(blockscale * 53, -1+i*(blockscale-1), 43*blockscale);
                c_enemy.stationary = false;
                c_enemy.final_angle = MathHelper.TwoPi + .001f;
                c_enemy.slowdownspeed = 2f;
                Spawn_Enemy(c_enemy);
            }
            Objects.Haunter_Enemy haunter = new Objects.Haunter_Enemy(m_Game);
            haunter.setPosition(new Vector3(57*blockscale, 0, 47*blockscale));
            haunter.variety = 1.0F;
            Spawn_Enemy(haunter);

            haunter = new Objects.Haunter_Enemy(m_Game);
            haunter.setPosition(new Vector3(49 * blockscale, 0, 47 * blockscale));
            haunter.variety = 0.9F;
            Spawn_Enemy(haunter);

            haunter = new Objects.Haunter_Enemy(m_Game);
            haunter.setPosition(new Vector3(50f, 0, 120f));
            haunter.variety = 0.9F;
            Spawn_Enemy(haunter);

            haunter = new Objects.Haunter_Enemy(m_Game);
            haunter.setPosition(new Vector3(49 * blockscale, 0, 34 * blockscale));
            haunter.variety = 0.8F;
            Spawn_Enemy(haunter);
            haunter = new Objects.Haunter_Enemy(m_Game);
            haunter.setPosition(new Vector3(60 * blockscale, 0, 35 * blockscale));
            haunter.variety = 0.95F;
            Spawn_Enemy(haunter);

            soul_crystal = new Objects.Soul_Crystal(m_Game);
            soul_crystal.setPosition(new Vector3(51 * blockscale, 4 * blockscale + 2, 32 * blockscale));
            Spawn_Item(soul_crystal);
            soul_crystal = new Objects.Soul_Crystal(m_Game);
            soul_crystal.setPosition(new Vector3(30f * blockscale, 2, 63 * blockscale));
            Spawn_Item(soul_crystal);
            haunter = new Objects.Haunter_Enemy(m_Game);
            haunter.setPosition(new Vector3(53 * blockscale, 0, 33 * blockscale));
            Spawn_Enemy(haunter);
            haunter = new Objects.Haunter_Enemy(m_Game);
            haunter.setPosition(new Vector3(49 * blockscale, 0, 33 * blockscale));
            Spawn_Enemy(haunter);
            haunter = new Objects.Haunter_Enemy(m_Game);
            haunter.setPosition(new Vector3(14 * blockscale, 0, 31 * blockscale));
            Spawn_Enemy(haunter);


            //left of spawn
            platform = new Objects.Platform(m_Game, 3F, 1, -5f, 0f, 5f, 0f, 0f, 6, 0f);
            platform.setPosition(new Vector3(59 * blockscale, 0, 23 * blockscale));
            SpawnGameObject(platform);
            m_platforms.Add(platform);
            soul_crystal = new Objects.Soul_Crystal(m_Game);
            soul_crystal.setPosition(new Vector3(59 * blockscale, 4.25f*blockscale, 23 * blockscale + 3));
            Spawn_Item(soul_crystal);

            //main hall
            for (int i = 0; i < 8; i++)
            {
                Objects.Thwomp_Enemy t_enemy = new Objects.Thwomp_Enemy(m_Game, (float)3f + i * .3f, false);
                t_enemy.setPosition(new Vector3(blockscale * 39, 5, 23 * blockscale + i * t_enemy.Scale * 10));
              
                Objects.Thwomp_Enemy t2_enemy = new Objects.Thwomp_Enemy(m_Game, (float)3f + i * .3f, false);
                t2_enemy.setPosition(new Vector3(blockscale * 41 , 5, 23 * blockscale + i * t_enemy.Scale * 10));
              
                Spawn_Enemy(t_enemy);
                Spawn_Enemy(t2_enemy);
            }
            soul_crystal = new Objects.Soul_Crystal(m_Game);
            soul_crystal.setPosition(new Vector3(40 * blockscale, 0f, 43 * blockscale));
            Spawn_Item(soul_crystal);

            Objects.Decoration decoration = new Objects.Decoration(m_Game, "Environment/Swinging_Doors");
            decoration.Scale = 0.6f;
            decoration.Position = new Vector3(0.5f+40 * blockscale, -decoration.Scale*2, 47 * blockscale);
            SpawnGameObject(decoration);
            ////Objects.Thwomp_Enemy t_enemyz = new Objects.Thwomp_Enemy(m_Game, (float)i * .3f);
            ////t_enemyz.setPosition(new Vector3(blockscale, 5, 10 * blockscale + i * t_enemyz.Scale * 10) + boospawn);
            ////Spawn_Enemy(t_enemyz);
            //Objects.Cannon_Enemy c_enemy = new Objects.Cannon_Enemy(m_Game);
            //c_enemy.Position = new Vector3(30, -1, 95) + boospawn;
            //c_enemy.final_angle = MathHelper.TwoPi + .001f;
            //Spawn_Enemy(c_enemy);
            //c_enemy = new Objects.Cannon_Enemy(m_Game);
            //c_enemy.Position = new Vector3(-30, -1, 80) + boospawn;
            //c_enemy.turning_left = true;
            //c_enemy.turn_left = true;
            //c_enemy.initial_angle = -001f;
            //c_enemy.final_angle = MathHelper.PiOver2 + .001f;
            //Spawn_Enemy(c_enemy);


            ////spawn the items
            //Objects.Soul_Crystal soul_crystal = new Objects.Soul_Crystal(m_Game);
            //soul_crystal.Position = new Vector3(0, -1, 80) + boospawn;
            //Spawn_Item(soul_crystal);
            //soul_crystal = new Objects.Soul_Crystal(m_Game);
            //soul_crystal.Position = new Vector3(1, -1, 80) + boospawn;
            //Spawn_Item(soul_crystal);
            //soul_crystal = new Objects.Soul_Crystal(m_Game);
            //soul_crystal.Position = new Vector3(-2, -1, 80) + boospawn;
            //Spawn_Item(soul_crystal);
            //soul_crystal = new Objects.Soul_Crystal(m_Game);
            //soul_crystal.Position = new Vector3(0, -1, 82) + boospawn;
            //Spawn_Item(soul_crystal);
            //soul_crystal = new Objects.Soul_Crystal(m_Game);
            //soul_crystal.Position = new Vector3(0, -1, 72) + boospawn;
            //Spawn_Item(soul_crystal);
            //soul_crystal = new Objects.Soul_Crystal(m_Game);
            //soul_crystal.Position = new Vector3(2, -1, 72) + boospawn;
            //Spawn_Item(soul_crystal);
            ////spawn the Decorations/Environment
            //Objects.Decoration decoration = new Objects.Decoration(m_Game, "Environment/PowerPoles");
            //decoration.Position = new Vector3(2, -1, 2);
            //Spawn_Item(decoration);
            //Objects.Decoration decoration2 = new Objects.Decoration(m_Game, "Environment/motion_platform");
            //decoration2.Scale = .4f;
            //decoration2.Position = boospawn + new Vector3(0, -1, 0);
            //Spawn_Item(decoration2);
        }

        #endregion

        #region Spawn and Destroy GameObjects

        public void Spawn_Enemy(Objects.Enemy enemy){
            m_enemies.Add(enemy);
            SpawnGameObject(enemy);
        }
        
        public void Destroy_Enemy(Objects.Enemy enemy)
        {
            m_enemies.Remove(enemy);
            RemoveGameObject(enemy);
        }
        
        public void Spawn_Item(Objects.Item item)
        {
            m_items.Add(item);
            SpawnGameObject(item);
        }
        
        public void Destroy_Item(Objects.Item item)
        {
            m_items.Remove(item);
            RemoveGameObject(item);
        }

        public void SpawnGameObject(GameObject o)
        {
            o.Load();
            m_GameObjects.AddLast(o);
            GraphicsManager.Get().AddGameObject(o);
        }
        public void SpawnTexture(Objects.Texture_Block b)
        {
            b.Load();
            GraphicsManager.Get().AddTile(b);
        }

        public void RemoveGameObject(GameObject o, bool bRemoveFromList = true)
        {
            o.Enabled = false;
            o.Unload();
            GraphicsManager.Get().RemoveGameObject(o);
            if (bRemoveFromList)
            {
                m_GameObjects.Remove(o);
            }
        }

        public void DestroyWorld() //Deletes everything besides boo
        {
            while (m_GameObjects.Count != 1)
            {
                if (m_GameObjects.ElementAt(0) != boo)
                {
                    RemoveGameObject(m_GameObjects.ElementAt(0));

                }
                else //The next indexes arent a boo
                {
                    RemoveGameObject(m_GameObjects.ElementAt(1));

                }
            }
        }

        #endregion

        #region Update

        public void Update(float fDeltaTime)
        {
            HandleStateChange();
            switch (m_State)
            {
                case eGameState.MainMenu:
                    UpdateMainMenu(fDeltaTime);
                    break;
                case eGameState.Gameplay:
                    UpdateGameplay(fDeltaTime);
                    break;
            }

            foreach (UI.UIScreen u in m_UIStack)
            {
                u.Update(fDeltaTime);
            }

            //destroy enemies
            //foreach (Objects.Enemy enem in m_enemies){
              //  if (enem.current_state == Objects.Enemy.State.EXPLODE_ME)
                //{
                  //  Destroy_Enemy(enem);
                //}
            //}
            // m_floor.Draw(m_Camera, effect);

            //Objects.ParticleEmitter explosion = new Objects.ParticleEmitter(m_Game);
            //explosion.AddExplosion(new Vector3(0, 0, -25), 100, 1, 2, fDeltaTime);

            //SpawnGameObject(explosion);
            //for (int i = 0; i < explosion.particleList.Count; i++)
            //{
              //  SpawnGameObject(explosion.particleList[i]);
            //}

        }

        void UpdateMainMenu(float fDeltaTime)
        {

        }

        bool first = true; //WHAT IS THIS FOR?
        void UpdateGameplay(float fDeltaTime)
        {
            if (!IsPaused)
            {
                m_Camera.Update(fDeltaTime);

                // Update objects in the world
                // We have to make a temp copy in case the objects list changes
                LinkedList<GameObject> temp = new LinkedList<GameObject>(m_GameObjects);
                foreach (GameObject o in temp)
                {
                    if (o.Enabled)
                    {
                        o.Update(fDeltaTime);
                    }
                }
                m_Timer.Update(fDeltaTime);

                /* UPDATE PARTICLE EMITTERS */
                for (int emitterCount = 0; emitterCount < particleEmitters.Count; emitterCount++)
                {
                    //particleEmitters[emitterCount].Update(fDeltaTime);
                }

                //collisions
                for (int i = 0; i < m_items.Count(); i++)
                {
                    if (m_items.ElementAt(i).m_WorldBounds.Intersects(boo.m_WorldBounds))
                    {
                        SoundManager.Get().PlaySoundCue("Collect");
                        Destroy_Item(m_items.ElementAt(i));
                        GameState.crystal_count++;
                        if (crystal_count >= 5) souls_obtained = true;
                        
                        break;
                    }
                }

                //fix for random update error
               // if (first && crystal_count == 2)
                //{
                  //  first = false;
                    //crystal_count = 0;

                //}
                //EDIT REGION to END GAME 
                if (DESTINATION_X_MIN <= boo.Position.X && DESTINATION_Z_MIN < boo.Position.Z && boo.Position.X <= DESTINATION_X_MAX && boo.Position.Z <= DESTINATION_Z_MAX&& crystal_count>=5)
                {
                    souls_obtained = true;
                }

                for (int i = 0; i < m_enemies.Count(); i++)
                {

                    Objects.Enemy enemy = m_enemies.ElementAt(i);
                    if (enemy.current_state == Objects.Enemy.State.EXPLODE_ME)
                    {
                            addExplosion(enemy.Position);
                            Destroy_Enemy(enemy);
                            goto AFTERLOOP;
                    }
                    if (enemy is Objects.Bomb_Enemy)
                    {
                        
                        if (boo.m_WorldBounds.Intersects(((Objects.Bomb_Enemy)enemy).m_WorldBounds) && boo.vulnerable)
                        {
                            boo.pain();
                            addExplosion(enemy.Position);
                            addBlood(boo.Position);
                            Destroy_Enemy(enemy);
                            goto AFTERLOOP;
                        }
                    }
                    if (enemy is Objects.Haunter_Enemy)
                    {
                        if (boo.m_WorldBounds.Intersects(((Objects.Haunter_Enemy)enemy).m_WorldBounds))
                        {
                            boo.pain();
                            addBlood(boo.Position);
                            goto AFTERLOOP;
                        }

                    }
                    if (enemy is Objects.Cannon_Enemy)
                    {
                        if (boo.m_WorldBounds.Intersects(((Objects.Cannon_Enemy)enemy).m_WorldBounds))
                        {
                            boo.pain();
                            addBlood(boo.Position);
                            goto AFTERLOOP;
                        }

                    }
                    if (enemy is Objects.Bullet_Enemy)
                    {
                        Objects.Bullet_Enemy b_enemy = (Objects.Bullet_Enemy)enemy;
                        if (b_enemy.m_WorldBounds.Intersects(boo.m_WorldBounds) && boo.vulnerable)
                        {
                            boo.pain();
                            addBlood(boo.Position);
                            Destroy_Enemy(b_enemy);
                            goto AFTERLOOP;
                        }
                        else
                        {
                            if (boo != null)
                            {



                            }
                            //search if it hits a wall
                            /*for (int x = 0; x < world_width; x++)
                            {
                                for (int z = 0; z < world_length; z++)
                                {
                                    for (int y = 0; y < world_height; y++)
                                    {
                                       if (m_tiles[x, y, z].m_WorldBounds.Intersects(b_enemy.m_WorldBounds))
                                        {
                                            Destroy_Enemy(b_enemy);
                                            goto AFTERLOOP;
                                        } 
                                    }
                                }
                            }*/

                            //search if it collides with another enemy
                            for (int j = 0; j < m_enemies.Count(); j++)
                            {
                                if (b_enemy != m_enemies.ElementAt(j) && (m_enemies.ElementAt(j) is Objects.Bullet_Enemy))
                                {
                                    //check for collision of two bullets
                                    Objects.Bullet_Enemy b2_enemy = (Objects.Bullet_Enemy)m_enemies.ElementAt(j);
                                    if (b_enemy.m_WorldBounds.Intersects(b2_enemy.m_WorldBounds))
                                    {
                                        addExplosion(b_enemy.Position);
                                        addExplosion(b2_enemy.Position);
                                        Destroy_Enemy(b_enemy);//destroy the two bullets
                                        Destroy_Enemy(b2_enemy);
                                        
                                        goto AFTERLOOP;
                                    }
                                }
                            }
                        }
                    }
                    //collisions for thwomp_enemy and boo or enemy
                    if (enemy is Objects.Thwomp_Enemy)
                    {
                        //collision with boo
                        Objects.Thwomp_Enemy t_enemy = (Objects.Thwomp_Enemy)enemy;
                        if (t_enemy.m_WorldBounds.Intersects(boo.m_WorldBounds) && boo.vulnerable)
                        {
                            addBlood(boo.Position);
                            boo.pain();
                            goto AFTERLOOP;

                        }
                        //collision with bullets
                        for (int j = 0; j < m_enemies.Count(); j++)
                        {
                            Objects.Enemy comparing_enemy = m_enemies.ElementAt(j);
                            if (comparing_enemy is Objects.Bullet_Enemy)
                            {
                                //kill bullet
                               Objects.Bullet_Enemy bu_enemy= (Objects.Bullet_Enemy)comparing_enemy;
                               if (bu_enemy.m_WorldBounds.Intersects(t_enemy.m_WorldBounds))
                               {
                                   addExplosion(bu_enemy.Position);
                                   Destroy_Enemy(bu_enemy);
                                   goto AFTERLOOP;
                               }
                            }
                            else if(comparing_enemy is Objects.Bomb_Enemy){
                                Objects.Bomb_Enemy bo_enemy = (Objects.Bomb_Enemy)comparing_enemy;
                                if (bo_enemy.m_WorldBounds.Intersects(t_enemy.m_WorldBounds))
                                {
                                    addExplosion(bo_enemy.Position);
                                    Destroy_Enemy(bo_enemy);
                                    goto AFTERLOOP;
                                }

                            }


                        }


                    }
                }

                 AFTERLOOP:
                            Console.Write("");

                // TODO: Any update code not for a specific game object should go here
            }
        }

        #endregion

        #region Input

        public void MouseClick(Point Position)
        {
            if (m_State == eGameState.Gameplay && !IsPaused)
            {
                // TODO: Respond to mouse clicks here
            }
        }

        
        public void KeyboardInput(SortedList<eBindings, BindInfo> binds, float fDeltaTime)
        {
            if (m_State == eGameState.Gameplay && !IsPaused)
            {
                // TODO: Add keyboard input handling for Gameplay
                if (binds.ContainsKey(eBindings.Boo_Left))
                {
                    boo.isTurn = Objects.Boo.direction.Boo_Left;
                }
                else if (binds.ContainsKey(eBindings.Boo_Right))
                {
                    boo.isTurn = Objects.Boo.direction.Boo_Right;
                }

                if (binds.ContainsKey(eBindings.Boo_Forward))
                {
                    boo.isMove = Objects.Boo.direction.Boo_Forward;
                }
                else if (binds.ContainsKey(eBindings.Boo_Backward))
                {
                    boo.isMove = Objects.Boo.direction.Boo_Backward;
                }

                if (binds.ContainsKey(eBindings.Boo_Jumped))
                {
                    boo.isJump = Objects.Boo.direction.Boo_Jumped;
                }
                else if (binds.ContainsKey(eBindings.Boo_Jump))
                {
                    boo.isJump = Objects.Boo.direction.Boo_Jump;
                }
                else if (binds.ContainsKey(eBindings.Boo_Letgo))
                {
                    boo.isJump = Objects.Boo.direction.Boo_Letgo;
                }
                if (binds.ContainsKey(eBindings.Reload_XML))
                {
                    XMLParser.Get().parseXML(m_tiles);
                }
            }
        }

        #endregion

        #region User Interface

        public UI.UIScreen GetCurrentUI()
        {
            return m_UIStack.Peek();
        }

        public int UICount
        {
            get { return m_UIStack.Count; }
        }

        // Has to be here because only this can access stack!
        public void DrawUI(float fDeltaTime, SpriteBatch batch)
        {
            // We draw in reverse so the items at the TOP of the stack are drawn after those on the bottom
            foreach (UI.UIScreen u in m_UIStack.Reverse())
            {
                u.Draw(fDeltaTime, batch);
            }
        }

        // Pops the current UI
        public void PopUI()
        {
            m_UIStack.Peek().OnExit();
            m_UIStack.Pop();
        }

        public void ShowPauseMenu()
        {
            IsPaused = true;
            m_UIStack.Push(new UI.UIPauseMenu(m_Game.Content));
        }

        public void Exit()
        {
            m_Game.Exit();
        }

        void GameOver(bool victorious)
        {
            IsPaused = true;
            m_UIStack.Push(new UI.UIGameOver(m_Game.Content, victorious));
        }

        #endregion

        #region Particles

        public void addExplosion(Vector3 explosionPosition)
        {
            if(Vector3.Distance(boo.Position,explosionPosition) < 65){
                Particles.ExplosionParticleSystem explosion = new Particles.ExplosionParticleSystem(m_Game, m_Game.Content);
                Particles.ExplosionSmokeParticleSystem explosionSmoke = new Particles.ExplosionSmokeParticleSystem(m_Game, m_Game.Content);
                explosion.Position = explosionPosition;
                explosionSmoke.Position = explosionPosition;
            SoundManager.Get().PlaySoundCue("Small_Explosion");
                m_Game.Components.Add(explosion);
                m_Game.Components.Add(explosionSmoke);
                m_Timer.AddTimer("Remove Explosion" + explosionCounter.ToString(), 2, removeExplosion, false);
                m_Timer.AddTimer("Remove Explosion Smoke" + explosionCounter.ToString(), 4, removeExplosionSmoke, false);

                explosionCounter++;
            }
        }

        public void removeExplosion()
        {
            for (int componentExplosionNumber = 0; componentExplosionNumber < m_Game.Components.Count; componentExplosionNumber++)
            {
                if (m_Game.Components[componentExplosionNumber].GetType() == typeof(Particles.ExplosionParticleSystem))
                {
                    m_Game.Components.RemoveAt(componentExplosionNumber);
                    break;
                }
            }
        }

        public void removeExplosionSmoke()
        {
            for (int componentSmokeNumber = 0; componentSmokeNumber < m_Game.Components.Count; componentSmokeNumber++)
            {
                if (m_Game.Components[componentSmokeNumber].GetType() == typeof(Particles.ExplosionSmokeParticleSystem))
                {
                    m_Game.Components.RemoveAt(componentSmokeNumber);
                    break;
                }
            }
        }

        public void addBlood(Vector3 bloodPosition)
        {
            Particles.BloodParticleSystem blood = new Particles.BloodParticleSystem(m_Game, m_Game.Content);
            blood.Position = bloodPosition;

            m_Game.Components.Add(blood);
            m_Timer.AddTimer("Remove Blood" + bloodCounter.ToString(), 1, removeBlood, false);
            bloodCounter++;
        }

        public void removeBlood()
        {
            for (int componentBloodNumber = 0; componentBloodNumber < m_Game.Components.Count; componentBloodNumber++)
            {
                if (m_Game.Components[componentBloodNumber].GetType() == typeof(Particles.BloodParticleSystem))
                {
                    m_Game.Components.RemoveAt(componentBloodNumber);
                    break;
                }
            }
        }

        #endregion
    }
}
