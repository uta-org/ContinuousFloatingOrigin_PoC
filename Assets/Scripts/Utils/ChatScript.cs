using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using CFO.Model;

namespace CFO.Utils
{
    public class ChatScript : MonoBehaviour
    {
        // Window will start 5 over and at the bottom of the screen, minus the height of the window
        // this puts it in the bottom left corner by default
        private Rect windowSize = new Rect(5, Screen.height - 255, 375, 250);

        // Can we currently move the window?
        private bool windowLocked = false;

        // Our textures for the lock and unlock icons
        public Texture lockIcon;

        public Texture unlockIcon;

        // Display the chat window?
        public bool showChat = true;

        // Are we showing the time in the chat window?
        private bool showChatTime = true;

        // Vector2 for the scroll view sizing
        private Vector2 scrollViewVector = Vector2.zero;

        // Some initial text strings
        private string innerText = "Inside the scrollview";

        private string chatString = "Enter text here";
        private string lastString = "Enter text here";

        // Chat color variables
        private int colorLength = 0;

        private string tmpColorStr = string.Empty;
        private bool showChatColor = true;

        //List<string> colors = new List<string>();
        private List<Colors> colorList = new List<Colors>()
        {
            new Colors("red", "#ff0000"),
            new Colors("blue", "#0000ff"),
            new Colors("green", "#00ff00"),
        };

        private struct Colors
        {
            public string colorName;
            public string colorValue;

            public Colors(string name, string value)
            {
                colorName = name;
                colorValue = value;
            }
        };

        private class ChatEntry
        {
            // Three elements to a chat message: Time, Name, and Text
            public DateTime timeStamp;

            public string name = "";
            public string text = "";

            public ChatEntry()
            {
                timeStamp = DateTime.Now;
                name = "Unknown";
                text = "Blank text";
            }

            // TimeStamp, Name, Text
            public ChatEntry(DateTime aTime, string aName, string aText)
            {
                timeStamp = aTime;
                name = aName;
                text = aText;
            }

            // Name, Text
            public ChatEntry(string aName, string aText)
            {
                name = aName;
                text = aText;
            }
        }

        // New list for our chat entries
        private List<ChatEntry> chatEntry = new List<ChatEntry>();

        // Use this for initialization
        private void Start()
        {
            //foreach (Colors tmpColor in colorList)
            //{
            //    Debug.Log(tmpColor.colorName + ", " + tmpColor.colorValue);
            //}

            // This will cause the chat window to be focused at start up
            //GUI.FocusControl("chatinput");
        }

        private void OnGUI()
        {
            // Create our window
            windowSize = GUI.Window(0, windowSize, chatWindow, "Chat");
        }

        private void chatWindow(int windowId)
        {
            // Show chat if its enabled
            if (showChat)
            {
                // Events for capturing mouse positions later for clicking lock/unlock
                Event e = Event.current;
                string tmpString = string.Empty;

                // Where our lock/unlock texture will be, based off the window size
                Rect lockPosition = new Rect(windowSize.width - 20, 2, 15, 15);
                // Draw the lock/unlock icon based on the bool if its locked
                GUI.DrawTexture(lockPosition, windowLocked ? lockIcon : unlockIcon);

                // Check if we clicked on the lock icon
                if (lockPosition.Contains(e.mousePosition))
                {
                    // Event checking, if we left clicked AND it was a mouse AND it was mouse down,
                    // then we toggle the lock/unlock state
                    if (e.button == 0 && e.isMouse && e.type == EventType.MouseDown)
                        windowLocked = !windowLocked;
                }

                // Sets our textfield name so we can focus it later
                GUI.SetNextControlName("chatinput");
                chatString = GUI.TextField(new Rect(2, windowSize.height - 32, windowSize.width - 4, 30), chatString,
                    400);

                // We hit esc, unfocus chat window
                if (e.keyCode == KeyCode.Escape)
                    GUI.FocusControl("");

                // We're not focused on the chat window, refocus it if we hit enter, and clear the chatstring/laststring
                if (GUI.GetNameOfFocusedControl() != "chatinput" && e.keyCode == KeyCode.Return)
                {
                    chatString = "";
                    lastString = chatString;
                    GUI.FocusControl("chatinput");
                }

                // Initial text string is not empty, and its not the first string then we do normal chat
                if (chatString != string.Empty && chatString != "Enter text here")
                {
                    // We've pressed enter, time to do stuff!
                    if (e.keyCode == KeyCode.Return)
                    {
                        // Send our string to the parser to see if it has a / in front for a command
                        ParseChatCommand(chatString);
                        // Add an entry to the list
                        chatEntry.Add(new ChatEntry(DateTime.Now, "Strom:", chatString));
                        // Set our laststring to our chatstring, this way we don't say the same thing over and over
                        lastString = chatString;
                        chatString = string.Empty;

                        //GUI.FocusControl("");
                    }
                }

                // Build our string from our chat entries
                foreach (ChatEntry tmpChat in chatEntry)
                {
                    // Can use color codes at the front of each chat line in two styles:
                    // First
                    // #red#This line will be red!

                    // Second
                    // #ff0000#This line will also be red!

                    // So far we only parse for 3 colors, red, green, and blue, but all RGB hex colors will work using the #xxyyzz# style
                    if (showChatColor)
                    {
                        // Check if our first character is a #
                        if (tmpChat.text.Substring(0, 1) == "#")
                        {
                            // Start of using a list of colors instead of hardcoded
                            //foreach (Colors tmpColor in colorList)
                            //{
                            //    if(tmpColor.colorName == tmpChat.text.Substring(0, 1)
                            //    Debug.Log(tmpColor.colorName + ", " + tmpColor.colorValue);
                            //}

                            // Now we check to see if we specified red and closed with a color character
                            if (tmpChat.text.Substring(1, 4).ToLower().Contains("red#"))
                            {
                                tmpColorStr = "#ff0000";
                                colorLength = 5;
                            }
                            // and green with a color character
                            else if (tmpChat.text.Substring(1, 6).ToLower().Contains("green#"))
                            {
                                tmpColorStr = "#00ff00";
                                colorLength = 7;
                            }
                            // and blue with a color character
                            else if (tmpChat.text.Substring(1, 5).ToLower().Contains("blue#"))
                            {
                                tmpColorStr = "#0000ff";
                                colorLength = 6;
                            }
                            // No word found from above, check for hex color and closing color character
                            else
                            {
                                // if have a format of #xxyyzz# then we can do a color
                                if (tmpChat.text.Substring(7, 1) == "#")
                                {
                                    tmpColorStr = tmpChat.text.Substring(0, 7);
                                    colorLength = 8;
                                }
                            }
                        }
                        else
                        {
                            tmpColorStr = "#ffffff";
                            colorLength = 0;
                        }

                        // Color and chat time
                        if (showChatTime)
                            tmpString += tmpChat.timeStamp + " " + tmpChat.name + " <color=" + tmpColorStr + ">" +
                                         tmpChat.text.Substring(colorLength) + "</color>\n";
                        else
                            // Color and no chat time
                            tmpString += tmpChat.name + " <color=" + tmpColorStr + ">" +
                                         tmpChat.text.Substring(colorLength) + "</color>\n";
                    }
                    // No color
                    else if (showChatTime)
                        tmpString += tmpChat.timeStamp + " " + tmpChat.name + " " + tmpChat.text + "\n";
                    else
                        // No color and no chat time
                        tmpString += tmpChat.name + " " + tmpChat.text + "\n";
                }

                GUIStyle textStyle = new GUIStyle
                {
                    normal =
                    {
                        textColor = Color.white
                    },
                    wordWrap = true,
                    richText = true
                };

                innerText = tmpString;

                // Set our scrollview name
                GUI.SetNextControlName("chatview"); // 2, windowSize.height - 32, windowSize.width - 4, 30
                scrollViewVector = GUI.BeginScrollView(new Rect(2, 18, windowSize.width - 10, 200), scrollViewVector,
                    new Rect(0, 0, windowSize.width - 28, 500));

                // Set our textarea to our huge chat string
                innerText = GUI.TextArea(new Rect(0, 0, windowSize.width - 24, 500), innerText, textStyle);

                // End the ScrollView
                GUI.EndScrollView();
            }

            // Allow window to be draggable if its not locked
            if (!windowLocked)
                GUI.DragWindow();
        }

        // Add an entry to our chatentry list
        private void AddChatEntry(DateTime aTime, string aName, string aText)
        {
            ChatEntry tmpEntry = new ChatEntry(aTime, aName, aText);
            chatEntry.Add(tmpEntry);
        }

        // Check if our chat command had a slash in it "/"
        private void ParseChatCommand(string command)
        {
            if (command.Length > 0)
                if (command[0] == '/')
                {
                    //AddChatEntry(DateTime.Now, "Strom", "Slash Command: " + command.Substring(1, command.Length - 1));
                    var commandName = command.Substring(1);
                    var commandParts = commandName.Split(' ');
                    switch (commandParts[0])
                    {
                        case "tp":
                            new TPCommand().Execute(commandParts.Skip(1).ToArray());
                            break;
                    }
                }
        }

        private void colorChatString()
        {
        }
    }
}