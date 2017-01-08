using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Server.Server;

namespace Server.GameSpecific
{
    public class GameView
    {
        public Vector2 ViewPosition = Vector2.Zero;
        public Vector2 ViewSize = Vector2.Zero;

        public GameView(Vector2 position, Vector2 size)
        {
            this.ViewPosition = position;
            this.ViewSize = size;
        }

        public void UpdateView(Vector2 pos, float dt, int clientId)
        {
            // Updated by the player that owns it and sent to client
            Vector2 prev = ViewPosition;
            ViewPosition = Vector2.Lerp(prev, pos, dt);

            float halfViewX = ViewSize.X * 0.5f;
            float halfViewY = ViewSize.Y * 0.5f;

            if (ViewPosition.X <= halfViewX)
            {
                ViewPosition.X = halfViewX;
            }
            else if (ViewPosition.X >= GameSimulation.instance.MapWidth() - halfViewX)
            {
                ViewPosition.X = GameSimulation.instance.MapWidth() - halfViewX;
            }

            if (ViewPosition.Y <= halfViewY)
            {
                ViewPosition.Y = halfViewY;
            }
            else if (ViewPosition.Y >= GameSimulation.instance.MapHeight() - halfViewY)
            {
                ViewPosition.Y = GameSimulation.instance.MapHeight() - halfViewY;
            }

            // Send packet of this calculated view centre
            PacketDefs.ViewUpdatePacket vp = new PacketDefs.ViewUpdatePacket(ViewPosition.X, ViewPosition.Y);
            ServerManager.instance.SendUdp(clientId, fastJSON.JSON.ToJSON(vp, PacketDefs.JsonParams()));
        }
    }
}
