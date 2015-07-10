using System;
using Terraria;
using TShockAPI;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using TerrariaApi.Server;
using System.Collections.Generic;

namespace StaffChatPlugin
{
    [ApiVersion(1, 19)]
    public class StaffChat : TerrariaPlugin
    {
        public static bool[] Spying = new bool[255];
        public static bool[] InStaffChat = new bool[255];

        public static Config config = new Config();

        Color staffchatcolor = new Color(200, 50, 150);

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public override string Author
        {
            get { return "Ancientgods"; }
        }
        public override string Name
        {
            get { return "StaffChat"; }
        }
        public override string Description
        {
            get { return "Allows staff to chat together in a group without other people seeing it."; }
        }

        public override void Initialize()
        {
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            Commands.ChatCommands.Add(new Command(StaffChat_Chat, "s") { AllowServer=false });
            Commands.ChatCommands.Add(new Command(Permission.Invite , StaffChat_Kick, "skick"));
            Commands.ChatCommands.Add(new Command(Permission.Invite, StaffChat_Invite, "sinvite"));
            Commands.ChatCommands.Add(new Command(Permission.Invite, StaffChat_Clear, "sclear"));
            Commands.ChatCommands.Add(new Command(Permission.List, StaffChat_List, "slist"));
            Commands.ChatCommands.Add(new Command(Permission.List, ShowStaff, "staff"));
            Commands.ChatCommands.Add(new Command(Permission.SpyWhisper, SpyWhisper, "spywhisper") { AllowServer=false });

            if (!File.Exists(Config.SavePath))
                config.Save();

            else config = Config.Load();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            }
            base.Dispose(disposing);
        }

        public StaffChat(Main game)
            : base(game)
        {
            Order = 1;
        }

        public void OnChat(ServerChatEventArgs args)
        {
            if ((args.Text.StartsWith("/w ") || args.Text.StartsWith("/whisper ") || args.Text.StartsWith("/r ") || args.Text.StartsWith("/reply ")) && args.Text.Length >= 4)
            {
                foreach (TSPlayer ts in TShock.Players)
                {
                    if (ts == null)
                        continue;

                    if (Spying[ts.Index])
                        ts.SendMessage(TShock.Players[args.Who].Name + ": " + args.Text, staffchatcolor);
                }
            }
        }

        public void OnLeave(LeaveEventArgs e)
        {
            Spying[e.Who] = false;
            InStaffChat[e.Who] = false;
        }

        private void StaffChat_Chat(CommandArgs args)
        {
            if (args.Parameters.Count <= 0)
            {
                args.Player.SendErrorMessage("Invalid syntax! proper syntax: /s <message>");
                return;
            }
            if (!args.Player.Group.HasPermission(Permission.Chat) && !InStaffChat[args.Player.Index])
            {
                args.Player.SendErrorMessage("You need to be invited to talk in the staffchat!");
                return;
            }
            foreach (TSPlayer ts in TShock.Players)
            {
                if (ts == null)
                    continue;

                if (ts.Group.HasPermission(Permission.Chat) || InStaffChat[args.Player.Index])
                {
                    string msg = string.Join("", args.Parameters);
                    ts.SendMessage(string.Format("{0}{1} {2}: {3}", config.StaffChatPrefix, (InStaffChat[args.Player.Index] ? " " + config.StaffChatGuestTag : ""), args.Player.Name, msg), staffchatcolor);
                }
            }
        }

        private void StaffChat_Invite(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Invalid syntax! Syntax: /sinvite <player>", Color.Red);
                return;
            }
            var foundplr = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (foundplr.Count == 0)
            {
                args.Player.SendMessage("Invalid player!", Color.Red);
            }
            else if (foundplr.Count > 1)
            {
                args.Player.SendMessage(string.Format("More than one ({0}) player matched!", foundplr.Count), Color.Red);
            }
            var plr = foundplr[0];

            if (plr.Group.HasPermission(Permission.Chat) || InStaffChat[plr.Index])
            {
                args.Player.SendErrorMessage("This player is already in the staffchat!");
                return;
            }
            InStaffChat[plr.Index] = true;
            plr.SendInfoMessage("You have been invited into the staffchat, type \"/s <message>\" to talk.");
            foreach (TSPlayer ts in TShock.Players)
            {
                if (ts == null)
                    continue;

                if (ts.Index == plr.Index || !ts.Group.HasPermission(Permission.Chat))
                    continue;

                ts.SendInfoMessage(plr.Name + " has been invited into the staffchat.");

            }
        }

        private void StaffChat_Kick(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Invalid syntax! Syntax: /skick <player>", Color.Red);
                return;
            }
            var foundplr = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (foundplr.Count == 0)
            {
                args.Player.SendMessage("Invalid player!", Color.Red);
            }
            else if (foundplr.Count > 1)
            {
                args.Player.SendMessage(string.Format("More than one ({0}) player matched!", foundplr.Count), Color.Red);
            }
            var plr = foundplr[0];

            if (!InStaffChat[plr.Index] || !plr.Group.HasPermission(Permission.Chat))
            {
                args.Player.SendErrorMessage("This player is not in the staffchat!");
                return;
            }
            if (plr.Group.HasPermission(Permission.Chat))
            {
                args.Player.SendErrorMessage("You can't kick another staff member from the staffchat!");
                return;
            }
            InStaffChat[plr.Index] = false;
            foreach (TSPlayer ts in TShock.Players)
            {
                if (ts == null)
                    continue;

                if (ts.Group.HasPermission(Permission.Chat) || InStaffChat[ts.Index])
                    ts.SendInfoMessage(plr.Name + " has been removed from the staffchat.");
            }
        }

        private void StaffChat_Clear(CommandArgs args)
        {
            foreach (TSPlayer ts in TShock.Players)
            {
                if (ts == null)
                    continue;

                if (InStaffChat[ts.Index])
                {
                    InStaffChat[ts.Index] = false;
                    ts.SendErrorMessage("You have been removed from the staffchat!");
                }

                else if (ts.Group.HasPermission(Permission.Chat))
                    ts.SendInfoMessage("All guests have been removed from the staffchat!");
            }
        }

        private void StaffChat_List(CommandArgs args)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < TShock.Players.Length; i++)
            {
                if (TShock.Players[i] == null)
                    continue;

                if (InStaffChat[TShock.Players[i].Index])
                    sb.Append(TShock.Players[i].Name + (i == TShock.Players.Length - 1 ? "" : ", "));

                string msg = sb.ToString();

                if (msg.Length > 0)
                    args.Player.SendInfoMessage("Guests in the staffchat: " + sb.ToString());
                else
                    args.Player.SendErrorMessage("There are no guests in the staffchat!");
            }
        }

        public void ShowStaff(CommandArgs args)
        {
            var staff = from staffmember in TShock.Players where staffmember != null && staffmember.Group.HasPermission(Permission.List) orderby staffmember.Group.Name select staffmember;
            args.Player.SendInfoMessage("~ Currently online staffmembers ~");
            foreach (TSPlayer ts in staff)
            {
                if (ts == null)
                    continue;

                Color groupcolor = new Color(ts.Group.R, ts.Group.G, ts.Group.B);
                args.Player.SendMessage(string.Format("{0}{1}", ts.Group.Prefix, ts.Name), groupcolor);
            }
        }

        private void SpyWhisper(CommandArgs args)
        {
            Spying[args.Player.Index] = !Spying[args.Player.Index];
            args.Player.SendInfoMessage(string.Format("You are {0} spying on whispers", Spying[args.Player.Index] ? "now" : "not"));
        }
    }
}

