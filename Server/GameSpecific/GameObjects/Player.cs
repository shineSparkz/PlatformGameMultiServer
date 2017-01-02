using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;


namespace Server.GameSpecific.GameObjects
{
    class Player : GameObject
    {
        const int iterations = 3;
        const int UP = 0;
        const int DOWN = 1;
        const int LEFT = 2;
        const int RIGHT = 3;

        private Point[] points;

        public Player(Vector2 p, GameObjectType obj_id, int unq_id, int isClient, bool updatable) :
            base(p, obj_id, unq_id, isClient, updatable)
        {
            this.CreateContactPoints();
        }

        public override void Update()
        {

            CheckCollisions();
            //position += gameObj.Velocity * dt;
            //int x = (int)position.X;
            //int y = (int)position.Y;
            //gameObj.Position = new Vector2(x,y);
            this.Position += this.Velocity;

            // Apply the force of gravity
            this.Velocity.Y += 0.981f;
        }

        private void CheckCollisions()
        {
            // ---- Resolve collision ----
            bool contactLeft = false, contactRight = false, contactYbottom = false, contactYtop = false;

            Vector2 predicted_speed = this.Velocity;;
            float projectedMoveX, projectedMoveY, originalMoveX, originalMoveY;
            originalMoveX = predicted_speed.X;
            originalMoveY = predicted_speed.Y;

            //Vector2 position = gameObj.Position;

            foreach (GameObject colTest in GameSimulation.instance.GetObjects())
            {
                if (colTest.TypeId() == GameObjectType.Wall)
                {
                    for (int dir = 0; dir < 4; dir++)
                    {
                        if (dir == UP && predicted_speed.Y > 0) continue;
                        if (dir == DOWN && predicted_speed.Y < 0) continue;
                        if (dir == LEFT && predicted_speed.X > 0) continue;
                        if (dir == RIGHT && predicted_speed.X < 0) continue;

                        projectedMoveX = (dir >= LEFT ? predicted_speed.X : 0);
                        projectedMoveY = (dir < LEFT ? predicted_speed.Y : 0);

                        while ((colTest.Bounds().Contains(this.points[dir * 2].X + (int)this.Position.X + (int)projectedMoveX,
                            this.points[dir * 2].Y + (int)this.Position.Y + (int)projectedMoveY)
                            ||
                            colTest.Bounds().Contains(this.points[dir * 2 + 1].X + (int)this.Position.X + (int)projectedMoveX,
                                this.points[dir * 2 + 1].Y + (int)this.Position.Y + (int)projectedMoveY)))
                        {
                            if (dir == UP)
                                projectedMoveY++;
                            if (dir == DOWN)
                                projectedMoveY--;
                            if (dir == LEFT)
                                projectedMoveX++;
                            if (dir == RIGHT)
                                projectedMoveX--;
                        }

                        if (dir >= LEFT && dir <= RIGHT)
                            predicted_speed.X = projectedMoveX;
                        if (dir >= UP && dir <= DOWN)
                            predicted_speed.Y = projectedMoveY;
                    }

                    // Resolve contact
                    if (predicted_speed.Y > originalMoveY && originalMoveY < 0)
                    {
                        contactYtop = true;
                    }

                    if (predicted_speed.Y < originalMoveY && originalMoveY > 0)
                    {
                        contactYbottom = true;
                    }

                    if (predicted_speed.X - originalMoveX < -0.01f)
                    {
                        contactRight = true;
                    }

                    if (predicted_speed.X - originalMoveX > 0.01f)
                    {
                        contactLeft = true;
                    }

                    // Resolve collision from contact
                    if (contactYbottom)
                    {
                        //position.Y += predicted_speed.Y;
                        this.Velocity.Y = 0;// = new Vector2(gameObj.Velocity.X, 0);
                        this.Grounded = true;
                    }
                    else if (contactYtop)
                    {
                        this.Velocity.Y = 0;
                    }

                    if (contactLeft || contactRight)
                    {
                        //position.X += predicted_speed.X;
                        this.Velocity.X = 0;// = new Vector2(0, gameObj.Velocity.Y);
                    }
                }
            }
        }

        private void CreateContactPoints()
        {
            if (this.m_TypeId == GameObjectType.Player)
            {
                points = new Point[8];

                points[0] = new Point(48, 38);
                points[1] = new Point(80, 38);

                points[2] = new Point(48, 128);
                points[3] = new Point(80, 128);

                points[4] = new Point(40, 58);
                points[5] = new Point(40, 108);

                points[6] = new Point(88, 58);
                points[7] = new Point(88, 108);
            }
        }
    }
}
