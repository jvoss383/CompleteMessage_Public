using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace CompleteMessage
{
    class Program
    {
        static void Main(string[] args)
        {
            string sender = "your name here";

            // getting locations of input files
            Console.WriteLine("Enter location of sms backup:");
            //string smsDirectory = Console.ReadLine().Replace("\"", " ");
            string smsDirectory = "E:\\Google Data Takeout 19 9 2019\\sms";

            Console.WriteLine("Enter location of instagram backup:");
            //string instaDirectory = Console.ReadLine().Replace("\"", " ");
            string instaDirectory = "E:\\Google Data Takeout 19 9 2019\\instagram\\glarson383_20200206_part_1\\messages.json";

            Console.WriteLine("Enter location of messenger backup: (facebook-genericname123465\\messages\\inbox\\)");
            //string messengerDirectory = Console.ReadLine().Replace("\"", " ");
            string messengerDirectory = "E:\\Google Data Takeout 19 9 2019\\facebook-greglarson718689\\messages\\inbox";

            Console.WriteLine("enter location of hangouts backup:");
            //string hangoutsDirectory = Console.ReadLine().Replace("\"", " ");
            string hangoutsDirectory = "E:\\Google Data Takeout 19 9 2019\\Hangouts\\Hangouts.json";

            Console.WriteLine("enter location of alias list (format .tsv`1 : given name, manual given name, alias a, alias b, alias c, alias...)");
            //string aliasDirectory = Console.ReadLine().Replace("\"", " ");
            string aliasDirectory = "E:\\Google Data Takeout 19 9 2019\\contactAliasesFULLManual.tsv";

            //smsDirectory = "";
            //instaDirectory = "";
            //messengerDirectory = "";
            //hangoutsDirectory = "";
            //aliasDirectory = "";
            
            List<Message> messages = new List<Message>();

            MessagingArchiveImport.ReadHangoutsJson(hangoutsDirectory, messages, true);
            MessagingArchiveImport.ReadMessengerJson(messengerDirectory, messages, true);
            MessagingArchiveImport.ReadInstagramJson(instaDirectory, messages, true);
            MessagingArchiveImport.ReadSmsXml(smsDirectory, messages, true, sender);
            MessagingArchiveImport.CorrectAliases(aliasDirectory, messages, true);

            if(false)
            {
                Console.WriteLine("reading hangouts data");
                if (hangoutsDirectory != "")
                {
                    dynamic hangoutsData = JsonConvert.DeserializeObject(File.ReadAllText(hangoutsDirectory));
                    Dictionary<string, string> gaiaIDs = new Dictionary<string, string>();
                    Dictionary<string, List<string>> conversationIDs = new Dictionary<string, List<string>>();

                    for (int conversationIndex = 0; conversationIndex < hangoutsData.conversations.Count; conversationIndex++)
                    {
                        // getting participant information
                        for (int participantDataIndex = 0; participantDataIndex < hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data.Count; participantDataIndex++)
                        {
                            string currentGaiaID = hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantDataIndex].id.chat_id.ToObject<string>();
                            // adds gaia ID to dictionary regardless of whether or not a fallback_name has been found
                            if (!gaiaIDs.ContainsKey(currentGaiaID))
                            {
                                gaiaIDs.Add(currentGaiaID, currentGaiaID);
                            }
                            // if fallback_name exists in current data tree path, add fallback_name to the current gaiaID entry
                            if (hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantDataIndex].ContainsKey("fallback_name"))
                            {
                                gaiaIDs[currentGaiaID] = hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantDataIndex].fallback_name.ToObject<string>();
                            }
                            else
                            {
                                Console.WriteLine("no fallback_name for gaiaID " + currentGaiaID);
                            }
                        }

                        // getting conversation information
                        string currentConversationID = hangoutsData.conversations[conversationIndex].conversation.conversation.id.id.ToObject<string>();
                        if (!conversationIDs.ContainsKey(currentConversationID))
                        {
                            conversationIDs.Add(currentConversationID, new List<string>());
                        }

                        for (int eventIndex = 0; eventIndex < hangoutsData.conversations[conversationIndex].events.Count; eventIndex++)
                        {
                            for (int participantIndex = 0; participantIndex < hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data.Count; participantIndex++)
                            {
                                if (!conversationIDs[currentConversationID].Contains(gaiaIDs[hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantIndex].id.gaia_id.ToObject<string>()]))
                                {
                                    conversationIDs[currentConversationID].Add(gaiaIDs[hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantIndex].id.gaia_id.ToObject<string>()]);
                                }
                            }

                            object exploration = hangoutsData.conversations[conversationIndex].events[eventIndex];
                            // if the event has a dynamic object called chat_message
                            if (hangoutsData.conversations[conversationIndex].events[eventIndex].ContainsKey("chat_message"))
                            {
                                object chat_messageObj = hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content;
                                // if the chat_message has a dynamic object called segment AND that segment has a dynamic object called text, otherwise see else if below
                                if (hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.ContainsKey("segment"))
                                {
                                    if (hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.segment[0].ContainsKey("text"))
                                    {
                                        if (!gaiaIDs.ContainsKey(hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()))
                                        {
                                            gaiaIDs.Add(hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>(), hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>());
                                        }
                                        object contentExplorer = hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.segment[0];
                                        string content = hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.segment[0].text.ToObject<string>();
                                        long ticks = new DateTime(1970, 1, 1).AddMilliseconds(hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>() / 1000).Ticks;
                                        string contact_name = gaiaIDs[hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()];
                                        string conversationID = currentConversationID;

                                        messages.Add(new Message(
                                            conversationIDs[currentConversationID].ToArray(),
                                            hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.segment[0].text.ToObject<string>(),
                                            //hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>(),
                                            new DateTime(1970, 1, 1).AddMilliseconds(hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>() / 1000).Ticks,
                                            "hangouts",
                                            gaiaIDs[hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()],
                                            currentConversationID));
                                    }
                                }
                                // else it could be an attachment type message, which is handled here using the dynamic object called attachment
                                else if (hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.ContainsKey("attachment"))
                                {
                                    /*string[] participants = conversationIDs[currentConversationID].ToArray();
                                    string url = hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.attachment[0].embed_item.plus_photo.original_content_url.ToObject<string>();
                                    long ticks = hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>();
                                    string senderID = gaiaIDs[hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()];*/

                                    /*messages.Add(new Message(
                                        conversationIDs[currentConversationID].ToArray(),
                                        hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.attachment[0].embed_item.plus_photo.original_content_url.ToObject<string>(),
                                        new DateTime(1970, 1, 1).AddMilliseconds(hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>() / 1000).Ticks,
                                        "hangouts",
                                        gaiaIDs[hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()],
                                        currentConversationID));*/
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("reading sms data...");
                // sms data extraction
                if (smsDirectory != "")
                {
                    for (int i = 0; i < Directory.GetFiles(smsDirectory).Count(); i++)
                    {
                        string[] smsData = File.ReadAllLines(Directory.GetFiles(smsDirectory)[i]);
                        for (int lineIndex = 0; lineIndex < smsData.Count(); lineIndex++)
                        {
                            // checks line is valid message
                            if (smsData[lineIndex].Length > 29 && smsData[lineIndex].Substring(0, 29) == "  <sms protocol=\"0\" address=\"")
                            {
                                // checks message type to see if I was the sender, or if they were
                                //string sender = ;
                                if (GetValueFromKey(smsData[lineIndex], "type") == "1")
                                {
                                    sender = GetValueFromKey(smsData[lineIndex], "contact_name");
                                }
                                Message curMessage = new Message(new string[] { GetValueFromKey(smsData[lineIndex], "contact_name") }, GetValueFromKey(smsData[lineIndex], "body"), /*Convert.ToInt64(GetValueFromKey(smsData[lineIndex], "date_sent"))*/ TimeZoneInfo.ConvertTimeToUtc(Convert.ToDateTime(GetValueFromKey(smsData[lineIndex], "readable_date"))).Ticks, "sms", sender, GetValueFromKey(smsData[lineIndex], "contact_name"));
                                messages.Add(curMessage);
                            }
                        }
                    }
                }

                Console.WriteLine("reading messenger data...");
                // messenger (facebook) data extraction
                if (messengerDirectory != "")
                {
                    // for each conversation
                    string[] conversations = Directory.GetDirectories(messengerDirectory);
                    for (int convoIndex = 0; convoIndex < conversations.Count(); convoIndex++)
                    {
                        dynamic jsonData = JsonConvert.DeserializeObject(File.ReadAllText(conversations[convoIndex] + "\\message_1.json"));
                        string groupTitle = jsonData.title.ToObject<string>();

                        // getting contacts
                        string[] contacts = new string[jsonData.participants.Count];
                        for (int contactIndex = 0; contactIndex < jsonData.participants.Count; contactIndex++)
                        {
                            contacts[contactIndex] = jsonData.participants[contactIndex].name.ToObject<string>();
                        }

                        // getting messages
                        for (int messageIndex = 0; messageIndex < jsonData.messages.Count; messageIndex++)
                        {
                            //string test = (string)jsonData.messages[messageIndex].content;
                            if (jsonData.messages[messageIndex].ContainsKey("content"))
                            {
                                messages.Add(new Message(
                                contacts,
                                jsonData.messages[messageIndex].content.ToObject<string>(),
                                //Convert.ToInt64(jsonData.messages[messageIndex].timestamp_ms.ToObject<string>()),
                                new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToInt64(jsonData.messages[messageIndex].timestamp_ms.ToObject<string>())).Ticks,
                                "messenger",
                                jsonData.messages[messageIndex].sender_name.ToObject<string>(),
                                jsonData.title.ToObject<string>())
                                );
                            }
                        }
                    }
                }

                Console.WriteLine("reading instagram data...");
                // getting instagram data
                if (instaDirectory != "")
                {
                    dynamic instaData = JsonConvert.DeserializeObject(File.ReadAllText(instaDirectory));
                    for (int convoIndex = 0; convoIndex < instaData.Count; convoIndex++)
                    {
                        string[] contacts = instaData[convoIndex].participants.ToObject<string[]>();
                        string groupName = "";
                        for (int i = 0; i < contacts.Count(); i++)
                        {
                            if (contacts[i] != "glarson383")
                                groupName += contacts[i] + " & ";
                        }
                        groupName = groupName.Substring(0, groupName.Length - 3);
                        dynamic conversation = instaData[convoIndex];
                        for (int messageIndex = 0; messageIndex < instaData[convoIndex].conversation.Count; messageIndex++)
                        {
                            if (instaData[convoIndex].conversation[messageIndex].ContainsKey("text"))
                            {
                                //string dateString = instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>();
                                //DateTime date = Convert.ToDateTime(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>());
                                //                              year                                                            month                                   day
                                /*DateTime date = new DateTime(
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[2].Split(' ')[0]), 
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[0]), 
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[1]), 
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[0]), 
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[1]), 
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[2]));*/

                                messages.Add(new Message(
                                    contacts,
                                    instaData[convoIndex].conversation[messageIndex].text.ToObject<string>(),
                                    new DateTime(
                                        Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[2].Split(' ')[0]),
                                        Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[0]),
                                        Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[1]),
                                        Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[0]),
                                        Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[1]),
                                        Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[2])).Ticks,
                                    "instagram",
                                    instaData[convoIndex].conversation[messageIndex].sender.ToObject<string>(),
                                    groupName));
                            }

                        }
                    }
                }

                // replacing contact names with aliases
                Console.WriteLine("correcting aliases...");
                if (aliasDirectory != "")
                {
                    // converting input data into a dictionary with pair (key = aliasX, value = givenName)
                    string[] aliasesRaw = File.ReadAllLines(aliasDirectory);
                    Dictionary<string, string> contactAliases = new Dictionary<string, string>();
                    for (int aliasRow = 1; aliasRow < aliasesRaw.Count(); aliasRow++)
                    {
                        string[] aliasRowSplit = aliasesRaw[aliasRow].Split('\t');
                        string givenName = aliasRowSplit[0];
                        for (int aliasColumn = 2; aliasColumn < aliasRowSplit.Count(); aliasColumn++) // starts at 2 to skip given_name and manual_given_name rows
                        {
                            if (aliasRowSplit[aliasColumn] != "" && !contactAliases.ContainsKey(aliasRowSplit[aliasColumn]))
                            {
                                contactAliases.Add(aliasRowSplit[aliasColumn], givenName);
                            }
                        }
                    }

                    // switching aliases in messages[] to given names
                    for (int messageIndex = 0; messageIndex < messages.Count(); messageIndex++)
                    {
                        // sender
                        if (contactAliases.ContainsKey(messages[messageIndex].sender))
                        {
                            messages[messageIndex].sender = contactAliases[messages[messageIndex].sender];
                        }
                        // participants
                        for (int contactIndex = 0; contactIndex < messages[messageIndex].contacts.Count(); contactIndex++)
                        {
                            if (contactAliases.ContainsKey(messages[messageIndex].contacts[contactIndex]))
                            {
                                messages[messageIndex].contacts[contactIndex] = contactAliases[messages[messageIndex].contacts[contactIndex]];
                            }
                        }
                    }
                }

            }
            
            /// At this stage all data has been collected into messages[]
            Console.WriteLine("all data collection complete");
            
            // messages by contact by day 
            Dictionary<string, List<Message>> messagesByContact = new Dictionary<string, List<Message>>();
            for(int messageIndex  = 0; messageIndex < messages.Count(); messageIndex++)
            {
                if(messages[messageIndex].contacts.Count() < 3)
                {
                    string contact = messages[messageIndex].contacts[0];
                    if(contact == sender)
                    {
                        if (messages[messageIndex].contacts.Count() > 1)
                        {
                            contact = messages[messageIndex].contacts[1];
                        }
                    }
                    if (!messagesByContact.ContainsKey(contact))
                    {
                        messagesByContact.Add(contact, new List<Message>());
                    }
                    messagesByContact[contact].Add(messages[messageIndex]);
                }
            }

            int days = 360 * 6;
            Dictionary<string, int[]> totalByDay = new Dictionary<string, int[]>();
            // loop for each contact
            using (StreamWriter sw = new StreamWriter("Output Conversation 201.txt"))
            {
                for (int contactIndex = 0; contactIndex < messagesByContact.Count(); contactIndex++)
                {
                    string currentContact = messagesByContact.ElementAt(contactIndex).Key;
                    // if dictionary doesn't have anything for this contact
                    if (!totalByDay.ContainsKey(currentContact))
                    {
                        totalByDay.Add(messagesByContact.ElementAt(contactIndex).Key, new int[days]);
                    }
                    // loop for each message inside of messagesByContact
                    for (int messageIndex = 0; messageIndex < messagesByContact.ElementAt(contactIndex).Value.Count(); messageIndex++)
                    {
                        int arrayIndex = (int)new DateTime(messagesByContact.ElementAt(contactIndex).Value[messageIndex].ticks).AddDays(days).Subtract(DateTime.Today.Date).TotalDays;
                        if (arrayIndex > 0 && arrayIndex < days && messagesByContact.ElementAt(contactIndex).Value[messageIndex].contacts.Count() < 3)
                        {
                            totalByDay[currentContact][arrayIndex]++;
                            if (arrayIndex == 1896 && messagesByContact.ElementAt(contactIndex).Key == "Person A")
                            {
                                if (messagesByContact.ElementAt(contactIndex).Value[messageIndex].sender == "Person B")
                                {
                                    sw.Write(new DateTime(messagesByContact.ElementAt(contactIndex).Value[messageIndex].ticks).TimeOfDay + " J:\t\t");
                                }
                                else
                                {
                                    sw.Write(new DateTime(messagesByContact.ElementAt(contactIndex).Value[messageIndex].ticks).TimeOfDay + " L: ");
                                }
                                DateTime date = new DateTime(messagesByContact.ElementAt(contactIndex).Value[messageIndex].ticks);
                                Console.Write(messagesByContact.ElementAt(contactIndex).Value[messageIndex].platform + " ");
                                sw.WriteLine("\t" + messagesByContact.ElementAt(contactIndex).Value[messageIndex].content);
                                Console.WriteLine(new DateTime(messagesByContact.ElementAt(contactIndex).Value[messageIndex].ticks).TimeOfDay + "\t" + messagesByContact.ElementAt(contactIndex).Value[messageIndex].content);
                            }
                        }
                    }
                }
            }


            // printing graphs, one per contact as sorted above
            for(int contactIndex = 0; contactIndex < totalByDay.Count(); contactIndex++)
            {
                Graph graph = new Graph(1920 * 2, 1080);
                graph.xDivCount = 72;
                graph.title = "Direct Messages Per Day Between <owner> and " + totalByDay.ElementAt(contactIndex).Key + " (hangouts, sms, messenger, instagram, 2018 - present)";
                graph.xLabel = "days previous to today";
                graph.yLabel = "Message Count";
                int firstMessageIndex = -1;
                int mostMessagesInADay = -1;
                int totalMessages = 0;
                for(int i = 0; i < totalByDay.ElementAt(contactIndex).Value.Count(); i++)
                {
                    totalMessages += totalByDay.ElementAt(contactIndex).Value[i];
                    if(totalByDay.ElementAt(contactIndex).Value[i] != 0 && firstMessageIndex == -1)
                    {
                        firstMessageIndex = i;
                    }
                    if(totalByDay.ElementAt(contactIndex).Value[i] > mostMessagesInADay)
                    {
                        mostMessagesInADay = totalByDay.ElementAt(contactIndex).Value[i];
                    }
                }
                if(mostMessagesInADay > 0)
                {
                    graph.maxYValue = mostMessagesInADay * 1.2f;
                    graph.minYValue = 0;
                    graph.minXValue = 0;
                    graph.maxXValue = days - firstMessageIndex;
                    //graph.rightMarginSizeMultiplier = 1.2f;

                    Color[] plotColors = new Color[] { Color.Black, Color.Red, Color.Cyan, Color.Yellow, Color.Blue, Color.Green, Color.Magenta, Color.MediumTurquoise, Color.Gray, Color.Tan };

                    graph.yDivCount = 10;
                    graph.BarY(ConvertArrayToFloat.Int(totalByDay.ElementAt(contactIndex).Value.Skip(firstMessageIndex).ToArray()), Color.FromArgb(64, 0, 0, 255), 1, totalByDay.ElementAt(contactIndex).Key + " (n = " + totalMessages + ")");
                    graph.DrawOutsideGraph();
                    graph.plot.Save(totalMessages + "_" + totalByDay.ElementAt(contactIndex).Key + "_direct.jpg", ImageFormat.Jpeg);
                }
            }//*/

            {
                /*int aTotal = 0;
                int bTotal = 0;
                for (int i = 0; i < totalByDay["Person A"].Count(); i++)
                {
                    bTotal += totalByDay["Person B"][i];
                    aTotal += totalByDay["Person A"][i];
                }

                Graph graph = new Graph(1920 * 2, 1080);
                graph.xDivCount = 72;
                graph.title = "Messages by Day and Contact (hangouts, sms, messenger, instagram, 2018 - present)";
                graph.xLabel = "days previous to today";
                graph.yLabel = "Message Count";
                graph.maxYValue = 900;
                graph.minYValue = 0;
                graph.minXValue = 0;
                graph.maxXValue = days;
                //graph.rightMarginSizeMultiplier = 1.2f;

                Color[] plotColors = new Color[] { Color.Black, Color.Red, Color.Cyan, Color.Yellow, Color.Blue, Color.Green, Color.Magenta, Color.MediumTurquoise, Color.Gray, Color.Tan };

                graph.yDivCount = 10;
                graph.BarY(ConvertArrayToFloat.Int(totalByDay["Person A"]), Color.FromArgb(64, 255, 0, 0), 1, "Person A     ( n = " + aTotal + " )");
                graph.BarY(ConvertArrayToFloat.Int(totalByDay["Person B"]), Color.FromArgb(64, 0, 0, 255), 1, "Person B( n = " + bTotal + " )");
                graph.DrawOutsideGraph();
                graph.plot.Save("plot.png", ImageFormat.Png);*/
            }

            // plotting messages by day of a given number of contacts over the period defined by days\
            {
            /*int days = 360 * 2;
            Dictionary<string, int[]> totalByDay = new Dictionary<string, int[]>();
            // loop for each contact
            for(int contactIndex = 0; contactIndex < messagesByContact.Count(); contactIndex++)
            {
                string currentContact = messagesByContact.ElementAt(contactIndex).Key;
                // if dictionary doesn't have anything for this contact
                if (!totalByDay.ContainsKey(currentContact))
                {
                    totalByDay.Add(messagesByContact.ElementAt(contactIndex).Key, new int[days]);
                }
                // loop for each message inside of messagesByContact
                for(int messageIndex = 0; messageIndex < messagesByContact.ElementAt(contactIndex).Value.Count(); messageIndex++)
                {
                    int arrayIndex = (int)new DateTime(messagesByContact.ElementAt(contactIndex).Value[messageIndex].ticks).AddDays(days).Subtract(DateTime.Today.Date).TotalDays;
                    if(arrayIndex > 0 && arrayIndex < days)
                    {
                        totalByDay[currentContact][arrayIndex]++;
                    }
                }
            }
            int contactsToPlot = 50;
            int[] xDataPlot = new int[days];
            for(int i = 0; i < days; i++)
            {
                xDataPlot[i] = i;
            }
            Dictionary<string, int[]> selectedTotalByDay = new Dictionary<string, int[]>();
            bool[] used = new bool[totalByDay.Count()];
            for(int i = 0; i < contactsToPlot; i++)
            {
                int largestFound = 0;
                int largestFoundIndex = 0;
                for (int contactIndex = 0; contactIndex < totalByDay.Count(); contactIndex++)
                {
                    if(!used[contactIndex] && totalByDay.ElementAt(contactIndex).Key != "Owner")
                    {
                        int sumOfCurrentContact = 0;
                        for (int j = 0; j < totalByDay.ElementAt(contactIndex).Value.Count(); j++)
                        {
                            sumOfCurrentContact += totalByDay.ElementAt(contactIndex).Value[j];
                        }
                        if (sumOfCurrentContact > largestFound)
                        {
                            largestFound = sumOfCurrentContact;
                            largestFoundIndex = contactIndex;
                        }
                    }
                }
                used[largestFoundIndex] = true;
                selectedTotalByDay.Add(totalByDay.ElementAt(largestFoundIndex).Key, totalByDay.ElementAt(largestFoundIndex).Value);
            }
            

            Graph graph = new Graph(1920 * 2, 1080);
            graph.xDivCount = 72;
            graph.title = "Messages by Day and Contact (hangouts, sms, messenger, instagram, 2018 - present)";
            graph.xLabel = "days previous to today";
            graph.yLabel = "Message Count";
            graph.maxYValue = 900;
            graph.minYValue = 0;
            graph.minXValue = 0;
            graph.maxXValue = days;
            //graph.rightMarginSizeMultiplier = 1.2f;

            Color[] plotColors = new Color[] { Color.Black, Color.Red, Color.Cyan, Color.Yellow, Color.Blue, Color.Green, Color.Magenta, Color.MediumTurquoise, Color.Gray, Color.Tan };

            graph.yDivCount = 10;
            for (int contactIndex = 0; contactIndex < contactsToPlot; contactIndex++)
            {
                graph.BarXY(ConvertArrayToFloat.Int(xDataPlot), ConvertArrayToFloat.Int(selectedTotalByDay.ElementAt(contactIndex).Value), Color.FromArgb( 64, plotColors[contactIndex % 10].R, plotColors[contactIndex % 10].G, plotColors[contactIndex % 10].B), 1, selectedTotalByDay.ElementAt(contactIndex).Key);
            }
            graph.DrawOutsideGraph();
            graph.plot.Save("plot.png", ImageFormat.Png);*/
            }

            {
                /*//getting grouping data into contacts, and then by time series messaging totals
                int timeDivisions = 256;
                Dictionary<string, int[]> contactsDict = new Dictionary<string, int[]>();
                for(int messageIndex = 0; messageIndex < messages.Count(); messageIndex++)
                {
                    if(messageIndex % 10000 == 0)
                    {
                        Console.WriteLine(messageIndex + " " + messages.Count() + " " + messageIndex / (float)messages.Count() * 100);
                    }

                    if (messages[messageIndex].content != null && messages[messageIndex].ticks > new DateTime(2018, 1, 1).Ticks)
                    {
                        if (!contactsDict.ContainsKey(messages[messageIndex].sender))
                        {
                            contactsDict.Add(messages[messageIndex].sender, new int[timeDivisions]);
                        }
                        contactsDict[messages[messageIndex].sender][(int)(new DateTime(messages[messageIndex].ticks).ToLocalTime().TimeOfDay.TotalDays * timeDivisions)]++;
                    }
                }

                // getting top x contacts with the most messages
                int topContactCount = 5;
                Tuple<string, int[]>[] contactTimeSeries = new Tuple<string, int[]>[topContactCount];
                int[] contactTotalMessages = new int[topContactCount];
                bool[] takenContacts = new bool[contactsDict.Count()];
                int largestSingleHourOfAnyContact = -1;
                // selecting top (total messages) contacts
                for(int i = 0; i < topContactCount; i++)
                {
                    int largestTotal = -1;
                    int largestIndex = -1;
                    // testing every contact from contactDict
                    for(int contactIndex = 0; contactIndex < contactsDict.Count(); contactIndex++)
                    {
                        // if not already selected
                        if(!takenContacts[contactIndex])
                        {
                            // finding total messages by contact
                            int localContactTotal = 0;
                            for(int timeIndex = 0; timeIndex < contactsDict.ElementAt(contactIndex).Value.Count(); timeIndex++)
                            {
                                localContactTotal += contactsDict.ElementAt(contactIndex).Value[timeIndex];
                                if(contactsDict.ElementAt(contactIndex).Value[timeIndex] > largestSingleHourOfAnyContact)
                                {
                                    largestSingleHourOfAnyContact = contactsDict.ElementAt(contactIndex).Value[timeIndex];
                                }
                            }
                            // if they're better than the current top
                            if(localContactTotal > largestTotal)
                            {
                                largestTotal = localContactTotal;
                                largestIndex = contactIndex;
                                contactTotalMessages[i] = largestTotal;
                            }
                        }
                    }
                    takenContacts[largestIndex] = true;
                    contactTimeSeries[i] = new Tuple<string, int[]>(contactsDict.ElementAt(largestIndex).Key, contactsDict.ElementAt(largestIndex).Value);
                    Console.WriteLine(i + " " + contactsDict.ElementAt(largestIndex).Key + " " + largestTotal);
                }

                Graph graph = new Graph((20 * 1920) / 23, 1080);
                graph.xDivCount = 24;
                graph.title = "Messages by Time of Day and Contact (hangouts, sms, messenger, instagram, 2018 - )";
                graph.xLabel = "Time of day (hours)";
                graph.yLabel = "Message Count";
                graph.maxYValue = largestSingleHourOfAnyContact;
                graph.minYValue = 0;
                graph.minXValue = 0;
                graph.maxXValue = 24;
                //graph.rightMarginSizeMultiplier = 1.2f;

                Color[] plotColors = new Color[] { Color.Black, Color.Red, Color.Cyan, Color.Yellow, Color.Blue, Color.Green, Color.Magenta, Color.MediumTurquoise, Color.Gray, Color.Tan };

                graph.yDivCount = 10;
                for (int contactIndex = 0; contactIndex < contactTimeSeries.Count(); contactIndex++)
                {
                    graph.LineY(ConvertArrayToFloat.Int(contactTimeSeries[contactIndex].Item2), plotColors[contactIndex], 2, contactTimeSeries[contactIndex].Item1 + " (n = " + contactTotalMessages[contactIndex] + ")");
                }
                graph.DrawOutsideGraph();
                graph.plot.Save("plot.png", ImageFormat.Png);
                */
            }

             Console.WriteLine();
            {/*int timeDivisions = 100;
            Dictionary<string, int[]> contactsByMessagesByTimeOfDay = new Dictionary<string, int[]>();
            for(int i = 0; i < messages.Count(); i++)
            {
                if(!contactsByMessagesByTimeOfDay.ContainsKey(messages[i].sender))
                {
                    contactsByMessagesByTimeOfDay.Add(messages[i].sender, new int[timeDivisions]);
                }
                contactsByMessagesByTimeOfDay[messages[i].sender][(int)Math.Floor(new DateTime(messages[i].ticks).ToLocalTime().TimeOfDay.TotalHours / 24f * timeDivisions)]++;
            }

            for(int i = 0; i < contactsByMessagesByTimeOfDay.Count(); i++)
            {
                int totalMessages = 0;
                for(int j = 0; j < timeDivisions; j++)
                {
                    totalMessages += contactsByMessagesByTimeOfDay.ElementAt(i).Value[j];
                }
                if(totalMessages < 500)
                {
                    contactsByMessagesByTimeOfDay.Remove(contactsByMessagesByTimeOfDay.ElementAt(i).Key);
                    i--;
                }
            }*/

                /*List<float> timeOfDay = new List<float>();
                List<float> messageLength = new List<float>();
                int timeDivisions = 100;
                float[] totalLengthByTime = new float[timeDivisions];
                float[] messagesByTime = new float[timeDivisions];

                for (int i = 0; i < messages.Count(); i++)
                {
                    //if(((int)new DateTime(messages[i].ticks).ToLocalTime().DayOfWeek > 1 && (int)new DateTime(messages[i].ticks).ToLocalTime().DayOfWeek < 6))
                    {
                        if (messages[i].content != null && messages[i].contacts.Count() < 3)
                        {
                            timeOfDay.Add((float)new DateTime(messages[i].ticks).ToLocalTime().TimeOfDay.TotalHours);
                            messageLength.Add(messages[i].content.Length);

                            totalLengthByTime[(int)Math.Floor(new DateTime(messages[i].ticks).ToLocalTime().TimeOfDay.TotalHours * (timeDivisions / 24f))] += messages[i].content.Length;
                            messagesByTime[(int)Math.Floor(new DateTime(messages[i].ticks).ToLocalTime().TimeOfDay.TotalHours * (timeDivisions / 24f))]++;
                        }
                    }
                }

                float[] averageLengthByTime = new float[timeDivisions];
                for(int i = 0; i < averageLengthByTime.Count(); i++)
                {
                    if(messagesByTime[i] != 0)
                    {
                        averageLengthByTime[i] = totalLengthByTime[i] / messagesByTime[i];
                    }
                    else
                    {
                        averageLengthByTime[i] = 0;
                    }
                }

                Graph graph = new Graph(3840, 2160);
                graph.xDivCount = 24;
                graph.title = "Message Length by Time of Day (All Days) (hangouts, sms, messenger, instagram) n = " + messageLength.Count();
                graph.xLabel = "Time of day (hours)";
                graph.yLabel = "Message Length (chars)";
                graph.maxYValue = 500;
                graph.yDivCount = 10;

                graph.CalculateXYMinMax(timeOfDay.ToArray(), messageLength.ToArray());
                graph.ScatterXY(timeOfDay.ToArray(), messageLength.ToArray(), Color.FromArgb(128, 0, 0, 0), 4, "Single Message");
                graph.LineY(averageLengthByTime, Color.FromArgb(255, 0, 0, 255), 2, "Average Message Length");
                graph.DrawOutsideGraph();
                graph.plot.Save("plot.png", ImageFormat.Png);*/
            }
        }

        static string GetValueFromKey (string inputData, string key)
        {
            for(int i = 0; i < inputData.Length - key.Length; i++)
            {
                if(inputData.Substring(i, key.Length) == key)
                {
                    return inputData.Substring(i + key.Length + 2, inputData.Length - i - key.Length - 2).Split('\"')[0];
                }
            }
            return null;
        }
    }

    class MessagingArchiveImport
    {
        public static List<Message> CorrectAliases (string aliasDirectory, List<Message> messages)
        {
            return CorrectAliases(aliasDirectory, messages, false);
        }

        public static List<Message> CorrectAliases (string aliasDirectory, List<Message> messages, bool verbose)
        {
            if (File.Exists(aliasDirectory))
            {
                // converting input data into a dictionary with pair (key = aliasX, value = givenName)
                string[] aliasesRaw = File.ReadAllLines(aliasDirectory);
                Dictionary<string, string> contactAliases = new Dictionary<string, string>();
                for (int aliasRow = 1; aliasRow < aliasesRaw.Count(); aliasRow++)
                {
                    string[] aliasRowSplit = aliasesRaw[aliasRow].Split('\t');
                    string givenName = aliasRowSplit[0];
                    for (int aliasColumn = 2; aliasColumn < aliasRowSplit.Count(); aliasColumn++) // starts at 2 to skip given_name and manual_given_name rows
                    {
                        if (aliasRowSplit[aliasColumn] != "" && !contactAliases.ContainsKey(aliasRowSplit[aliasColumn]))
                        {
                            contactAliases.Add(aliasRowSplit[aliasColumn], givenName);
                        }
                    }
                }
                if (verbose)
                {
                    Console.WriteLine(contactAliases.Count() + " aliases found");
                }

                // switching aliases in messages[] to given names
                for (int messageIndex = 0; messageIndex < messages.Count(); messageIndex++)
                {
                    // sender
                    if (contactAliases.ContainsKey(messages[messageIndex].sender))
                    {
                        messages[messageIndex].sender = contactAliases[messages[messageIndex].sender];
                    }
                    // participants
                    for (int contactIndex = 0; contactIndex < messages[messageIndex].contacts.Count(); contactIndex++)
                    {
                        if (contactAliases.ContainsKey(messages[messageIndex].contacts[contactIndex]))
                        {
                            messages[messageIndex].contacts[contactIndex] = contactAliases[messages[messageIndex].contacts[contactIndex]];
                        }
                    }
                }
            }
            else if (verbose)
            {
                Console.WriteLine("could not find file " + aliasDirectory);
            }
            return messages;
        }

        public static List<Message> ReadInstagramJson(string instaDirectory)
        {
            return ReadInstagramJson(instaDirectory, new List<Message>(), false);
        }

        public static List<Message> ReadInstagramJson(string instaDirectory, List<Message> messages)
        {
            return ReadInstagramJson(instaDirectory, messages, false);
        }

        public static List<Message> ReadInstagramJson(string instaDirectory, List<Message> messages, bool verbose)
        {
            if (File.Exists(instaDirectory))
            {
                if (verbose)
                {
                    Console.WriteLine("instagram file \"" + instaDirectory + "\" found");
                }
                dynamic instaData = JsonConvert.DeserializeObject(File.ReadAllText(instaDirectory));
                for (int convoIndex = 0; convoIndex < instaData.Count; convoIndex++)
                {
                    string[] contacts = instaData[convoIndex].participants.ToObject<string[]>();
                    string groupName = "";
                    for (int i = 0; i < contacts.Count(); i++)
                    {
                        if (contacts[i] != "glarson383")
                            groupName += contacts[i] + " & ";
                    }
                    groupName = groupName.Substring(0, groupName.Length - 3);
                    dynamic conversation = instaData[convoIndex];
                    if (verbose)
                    {
                        Console.WriteLine("convoIndex: " + convoIndex + " " + groupName);
                    }
                    for (int messageIndex = 0; messageIndex < instaData[convoIndex].conversation.Count; messageIndex++)
                    {
                        if (instaData[convoIndex].conversation[messageIndex].ContainsKey("text"))
                        {
                            //string dateString = instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>();
                            //DateTime date = Convert.ToDateTime(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>());
                            //                              year                                                            month                                   day
                            /*DateTime date = new DateTime(
                                Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[2].Split(' ')[0]), 
                                Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[0]), 
                                Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[1]), 
                                Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[0]), 
                                Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[1]), 
                                Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[2]));*/

                            messages.Add(new Message(
                                contacts,
                                instaData[convoIndex].conversation[messageIndex].text.ToObject<string>(),
                                new DateTime(
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[2].Split(' ')[0]),
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[0]),
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split('/')[1]),
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[0]),
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[1]),
                                    Convert.ToInt32(instaData[convoIndex].conversation[messageIndex].created_at.ToObject<string>().Split(' ')[1].Split(':')[2])).Ticks,
                                "instagram",
                                instaData[convoIndex].conversation[messageIndex].sender.ToObject<string>(),
                                groupName));
                        }

                    }
                }
            }
            else if (verbose)
            {
                Console.WriteLine("instagram file \"" + instaDirectory + "\" does not exist");
            }
            return messages;
        }

        public static List<Message> ReadMessengerJson(string messengerDirectory)
        {
            return ReadHangoutsJson(messengerDirectory, new List<Message>(), false);
        }

        public static List<Message> ReadMessengerJson(string messengerDirectory, List<Message> messages)
        {
            return ReadHangoutsJson(messengerDirectory, messages, false);
        }

        public static List<Message> ReadMessengerJson(string messengerDirectory, List<Message> messages, bool verbose)
        {
            if(Directory.Exists(messengerDirectory))
            {
                int messageCount = 0;
                // for each conversation
                string[] conversations = Directory.GetDirectories(messengerDirectory);
                if (verbose)
                {
                    Console.WriteLine(Directory.GetDirectoryRoot(messengerDirectory) + " files found in " + messengerDirectory);
                }
                for (int convoIndex = 0; convoIndex < conversations.Count(); convoIndex++)
                {
                    if (verbose)
                    {
                        Console.WriteLine("reading file " + conversations[convoIndex] + "\\messages_1.json");
                    }
                    dynamic jsonData = JsonConvert.DeserializeObject(File.ReadAllText(conversations[convoIndex] + "\\message_1.json"));
                    string groupTitle = jsonData.title.ToObject<string>();
                    if (verbose)
                    {
                        Console.WriteLine("read complete");
                    }

                    // getting contacts
                    string[] contacts = new string[jsonData.participants.Count];
                    for (int contactIndex = 0; contactIndex < jsonData.participants.Count; contactIndex++)
                    {
                        contacts[contactIndex] = jsonData.participants[contactIndex].name.ToObject<string>();
                    }

                    // getting messages
                    for (int messageIndex = 0; messageIndex < jsonData.messages.Count; messageIndex++)
                    {
                        //string test = (string)jsonData.messages[messageIndex].content;
                        if (jsonData.messages[messageIndex].ContainsKey("content"))
                        {
                            messages.Add(new Message(
                            contacts,
                            jsonData.messages[messageIndex].content.ToObject<string>(),
                            //Convert.ToInt64(jsonData.messages[messageIndex].timestamp_ms.ToObject<string>()),
                            new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToInt64(jsonData.messages[messageIndex].timestamp_ms.ToObject<string>())).Ticks,
                            "messenger",
                            jsonData.messages[messageIndex].sender_name.ToObject<string>(),
                            jsonData.title.ToObject<string>())
                            );
                            messageCount++;
                        }
                    }
                }
                if (verbose)
                {
                    Console.WriteLine(messageCount + " total messages found");
                }
            }
            else if (verbose)
            {
                Console.WriteLine("directory " + messengerDirectory + " does not exist");
            }
            return messages;
        }

        public static List<Message> ReadHangoutsJson(string hangoutsDirectory)
        {
            return ReadHangoutsJson(hangoutsDirectory, new List<Message>(), false);
        }

        public static List<Message> ReadHangoutsJson(string hangoutsDirectory, List<Message> messages)
        {
            return ReadHangoutsJson(hangoutsDirectory, messages, false);
        }

        public static List<Message> ReadHangoutsJson(string hangoutsDirectory, List<Message> messages, bool verbose)
        {
            if (verbose)
            {
                Console.WriteLine("entering read hangouts json...");
            }
            if (File.Exists(hangoutsDirectory))
            {
                if(verbose)
                {
                    Console.WriteLine("file " + hangoutsDirectory + "confirmed to exist");
                    Console.WriteLine("reading to dynamic JArray...");
                }
                dynamic hangoutsData = JsonConvert.DeserializeObject(File.ReadAllText(hangoutsDirectory));
                Dictionary<string, string> gaiaIDs = new Dictionary<string, string>();
                Dictionary<string, List<string>> conversationIDs = new Dictionary<string, List<string>>();
                int participantCount = 0;
                int conversationCount = 0;
                int messageCount = 0;
                if (verbose)
                {
                    Console.WriteLine("JArray created");
                }
                for (int conversationIndex = 0; conversationIndex < hangoutsData.conversations.Count; conversationIndex++)
                {
                    if (verbose)
                    {
                        Console.WriteLine("reading conversation at index: " + conversationIndex);
                    }
                    // getting participant information
                    for (int participantDataIndex = 0; participantDataIndex < hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data.Count; participantDataIndex++)
                    {
                        string currentGaiaID = hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantDataIndex].id.chat_id.ToObject<string>();
                        // adds gaia ID to dictionary regardless of whether or not a fallback_name has been found
                        if (!gaiaIDs.ContainsKey(currentGaiaID))
                        {
                            gaiaIDs.Add(currentGaiaID, currentGaiaID);
                            participantCount++;
                        }
                        // if fallback_name exists in current data tree path, add fallback_name to the current gaiaID entry
                        if (hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantDataIndex].ContainsKey("fallback_name"))
                        {
                            gaiaIDs[currentGaiaID] = hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantDataIndex].fallback_name.ToObject<string>();
                        }
                        else
                        {
                            Console.WriteLine("no fallback_name for gaiaID " + currentGaiaID);
                        }
                    }



                    // getting conversation information
                    string currentConversationID = hangoutsData.conversations[conversationIndex].conversation.conversation.id.id.ToObject<string>();
                    if (!conversationIDs.ContainsKey(currentConversationID))
                    {
                        conversationIDs.Add(currentConversationID, new List<string>());
                        conversationCount++;
                    }

                    for (int eventIndex = 0; eventIndex < hangoutsData.conversations[conversationIndex].events.Count; eventIndex++)
                    {
                        for (int participantIndex = 0; participantIndex < hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data.Count; participantIndex++)
                        {
                            if (!conversationIDs[currentConversationID].Contains(gaiaIDs[hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantIndex].id.gaia_id.ToObject<string>()]))
                            {
                                conversationIDs[currentConversationID].Add(gaiaIDs[hangoutsData.conversations[conversationIndex].conversation.conversation.participant_data[participantIndex].id.gaia_id.ToObject<string>()]);
                            }
                        }

                        object exploration = hangoutsData.conversations[conversationIndex].events[eventIndex];
                        // if the event has a dynamic object called chat_message
                        if (hangoutsData.conversations[conversationIndex].events[eventIndex].ContainsKey("chat_message"))
                        {
                            object chat_messageObj = hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content;
                            // if the chat_message has a dynamic object called segment AND that segment has a dynamic object called text, otherwise see else if below
                            if (hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.ContainsKey("segment"))
                            {
                                if (hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.segment[0].ContainsKey("text"))
                                {
                                    if (!gaiaIDs.ContainsKey(hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()))
                                    {
                                        gaiaIDs.Add(hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>(), hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>());
                                    }
                                    object contentExplorer = hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.segment[0];
                                    string content = hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.segment[0].text.ToObject<string>();
                                    long ticks = new DateTime(1970, 1, 1).AddMilliseconds(hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>() / 1000).Ticks;
                                    string contact_name = gaiaIDs[hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()];
                                    string conversationID = currentConversationID;

                                    messageCount++;
                                    messages.Add(new Message(
                                        conversationIDs[currentConversationID].ToArray(),
                                        hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.segment[0].text.ToObject<string>(),
                                        //hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>(),
                                        new DateTime(1970, 1, 1).AddMilliseconds(hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>() / 1000).Ticks,
                                        "hangouts",
                                        gaiaIDs[hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()],
                                        currentConversationID));
                                }
                            }
                            // else it could be an attachment type message, which is handled here using the dynamic object called attachment
                            else if (hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.ContainsKey("attachment"))
                            {
                                /*string[] participants = conversationIDs[currentConversationID].ToArray();
                                string url = hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.attachment[0].embed_item.plus_photo.original_content_url.ToObject<string>();
                                long ticks = hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>();
                                string senderID = gaiaIDs[hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()];*/

                                /*messages.Add(new Message(
                                    conversationIDs[currentConversationID].ToArray(),
                                    hangoutsData.conversations[conversationIndex].events[eventIndex].chat_message.message_content.attachment[0].embed_item.plus_photo.original_content_url.ToObject<string>(),
                                    new DateTime(1970, 1, 1).AddMilliseconds(hangoutsData.conversations[conversationIndex].events[eventIndex].timestamp.ToObject<long>() / 1000).Ticks,
                                    "hangouts",
                                    gaiaIDs[hangoutsData.conversations[conversationIndex].events[eventIndex].sender_id.gaia_id.ToObject<string>()],
                                    currentConversationID));*/
                            }
                        }
                    }
                }
                if(verbose)
                {
                    Console.WriteLine("read hangouts json complete\nfound:\n" + conversationCount + " conversations\n" + participantCount + " participants\n" + messageCount + " messages");
                }
            }
            else if (verbose)
            {
                Console.WriteLine("file at " + hangoutsDirectory + "does not exist\nexiting module...");
            }
            return messages;
        }

        public static List<Message> ReadSmsXml(string smsDirectory, string sender)
        {
            return ReadSmsXml(smsDirectory, new List<Message>(), false, sender);
        }

        public static List<Message> ReadSmsXml(string smsDirectory, List<Message> messages, string sender)
        {
            return ReadSmsXml(smsDirectory, messages, false, sender);
        }

        public static List<Message> ReadSmsXml(string smsDirectory, List<Message> messages, bool verbose, string sender)
        {
            if (Directory.Exists(smsDirectory))
            {
                int messageCount = 0;
                if(verbose)
                {
                    Console.WriteLine("found " + Directory.GetFiles(smsDirectory).Count() + " files at " + smsDirectory);
                }
                for (int i = 0; i < Directory.GetFiles(smsDirectory).Count(); i++)
                {
                    if (verbose)
                    {
                        Console.WriteLine("reading " + Directory.GetFiles(smsDirectory)[i]);
                    }
                    string[] smsData = File.ReadAllLines(Directory.GetFiles(smsDirectory)[i]);
                    for (int lineIndex = 0; lineIndex < smsData.Count(); lineIndex++)
                    {
                        // checks line is valid message
                        if (smsData[lineIndex].Length > 29 && smsData[lineIndex].Substring(0, 29) == "  <sms protocol=\"0\" address=\"")
                        {
                            // checks message type to see if I was the sender, or if they were
                            //string sender = "James Voss";
                            if (GetValueFromKey(smsData[lineIndex], "type") == "1")
                            {
                                sender = GetValueFromKey(smsData[lineIndex], "contact_name");
                            }
                            Message curMessage = new Message(new string[] { GetValueFromKey(smsData[lineIndex], "contact_name") }, GetValueFromKey(smsData[lineIndex], "body"), /*Convert.ToInt64(GetValueFromKey(smsData[lineIndex], "date_sent"))*/ TimeZoneInfo.ConvertTimeToUtc(Convert.ToDateTime(GetValueFromKey(smsData[lineIndex], "readable_date"))).Ticks, "sms", sender, GetValueFromKey(smsData[lineIndex], "contact_name"));
                            messages.Add(curMessage);
                        }
                    }
                }
                if (verbose)
                {
                    Console.WriteLine(messageCount + " messages found");
                }
            }
            return messages;
        }

        private static string GetValueFromKey(string inputData, string key)
        {
            for (int i = 0; i < inputData.Length - key.Length; i++)
            {
                if (inputData.Substring(i, key.Length) == key)
                {
                    return inputData.Substring(i + key.Length + 2, inputData.Length - i - key.Length - 2).Split('\"')[0];
                }
            }
            return null;
        }
    }

    class ConvertArrayToFloat
    {
        public static float[] Int (int[] input)
        {
            float[] output = new float[input.Count()];
            for(int i = 0; i < input.Count(); i++)
            {
                output[i] = input[i];
            }
            return output;
        }
    }

    class Contact
    {
        public string name;
        public long sentChars;
        public long recChars;
        public long sentWords;
        public long recWords;
        public long sentMessages;
        public long recMessages;

        public Contact(string name)
        {
            this.name = name;
            
            this.sentChars = 0;
            this.sentWords = 0;
            this.sentMessages = 0;

            this.recChars = 0;
            this.recMessages = 0;
            this.recWords = 0;
        }

        public Contact (string name, long sentChars, long sentWords, long sentMessages, long recChars, long recWords, long recMessages)
        {
            this.name = name;
            this.sentChars = sentChars;
            this.sentWords = sentWords;
            this.sentMessages = sentMessages;

            this.recChars = recChars;
            this.recMessages = recMessages;
            this.recWords = recWords;
        }
    }

    class Message
    {
        public string sender;
        public string[] contacts;
        public string content;
        public long ticks;
        public string platform;
        public string groupTitle;

        public Message(string[] contacts, string content, long ticks, string platform, string sender, string groupTitle)
        {
            this.sender = sender;
            this.contacts = contacts;
            this.ticks = ticks;
            this.content = content;
            this.platform = platform;
            this.groupTitle = groupTitle;
        }
    }
    
    class LegendEntry
    {
        public enum PlotTypes
        {
            FillEllipse = 0,
            DrawEllipse = 1,
            DrawLine = 2,
            FillRectangle = 3
        }

        public PlotTypes plotType = PlotTypes.FillEllipse;
        public string title = "";
        public Color color = Color.Black;
        public float plotSize = 3;

        public LegendEntry(PlotTypes plotType, string title, Color color, float plotSize)
        {
            this.plotType = plotType;
            this.title = title;
            this.color = color;
            this.plotSize = plotSize;
        }
    }

    class Graph
    {
        public Image plot;
        public int plotWidth = 1920 - 200;
        public int extendedPlotWidth = 1920;
        public float rightMarginSizeMultiplier = 3f;
        public int plotHeight = 1080 - 200;
        public float margins = float.MinValue;
        public float maxVerticalScale = 1;
        public int xDivCount = 10;
        public int yDivCount = 10;
        public string title = "title";
        public string xLabel = "xLabel";
        public string yLabel = "yLabel";
        public float maxYValue = float.MinValue;
        public float minYValue = float.MinValue;
        public float maxXValue = float.MinValue;
        public float minXValue = float.MinValue;
        List<LegendEntry> legend = new List<LegendEntry>();

        public Graph(int plotWidth, int plotHeight)
        {
            this.plotWidth = plotWidth;
            this.plotHeight = plotHeight;
            if (plotHeight < plotWidth)
            {
                margins = plotHeight / 10;
            }
            else
            {
                margins = plotWidth / 10;
            }
            extendedPlotWidth = (int)(plotWidth + margins * (rightMarginSizeMultiplier - 1));
            plot = new Bitmap(extendedPlotWidth, plotHeight);
            using (Graphics g = Graphics.FromImage(plot))
            {
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, extendedPlotWidth, plotHeight);
            }
        }

        public void BarXY(float[] x, float[] y, Color plotColor, float plotSize, string seriesTitle)
        {
            legend.Add(new LegendEntry(LegendEntry.PlotTypes.FillRectangle, seriesTitle, plotColor, plotSize));

            using (Graphics g = Graphics.FromImage(plot))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                for (int i = 0; i < x.Count() - 1 && i < y.Count() - 1; i++)
                {
                    PointF pointA = ScaleToBounds(new PointF(x[i], y[i]));
                    PointF pointB = ScaleToBounds(new PointF(x[i + 1], y[i + 1]));
                    //g.DrawLine(new Pen(new SolidBrush(Color.FromArgb(128, 255, 255, 255)), lineThickness * 5), pointA, pointB);
                    g.FillPolygon(new SolidBrush(plotColor), new PointF[] { pointA, new PointF(pointB.X, pointA.Y), new PointF(pointB.X, plotHeight - margins), new PointF(pointA.X, plotHeight - margins) });

                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), pointA, new PointF(pointB.X, pointA.Y));
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), pointA, new PointF(pointB.X, pointA.Y));
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), pointA, new PointF(pointB.X, pointA.Y));
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), pointA, new PointF(pointB.X, pointA.Y));
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), pointA, new PointF(pointB.X, pointA.Y));
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), pointA, new PointF(pointB.X, pointA.Y));

                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), new PointF(pointB.X, pointA.Y), pointB);
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), new PointF(pointB.X, pointA.Y), pointB);
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), new PointF(pointB.X, pointA.Y), pointB);
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), new PointF(pointB.X, pointA.Y), pointB);
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), new PointF(pointB.X, pointA.Y), pointB);
                    g.DrawLine(new Pen(new SolidBrush(plotColor), plotSize), new PointF(pointB.X, pointA.Y), pointB);
                }
            }
        }

        public void BarY(float[] y, Color plotColor, float plotSize, string seriesTitle)
        {
            float[] x = new float[y.Count()];
            for (int i = 0; i < x.Count(); i++)
            {
                x[i] = i / (float)x.Count() * (maxXValue - minXValue) + minXValue;
            }
            BarXY(x, y, plotColor, plotSize, seriesTitle);
        }

        public void AreaXY(float[] x, float[] y, Color plotColor, float plotSize, string seriesTitle)
        {
            legend.Add(new LegendEntry(LegendEntry.PlotTypes.FillRectangle, seriesTitle, plotColor, plotSize));

            using (Graphics g = Graphics.FromImage(plot))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                for (int i = 0; i < x.Count() - 1 && i < y.Count() - 1; i++)
                {
                    PointF pointA = ScaleToBounds(new PointF(x[i], y[i]));
                    PointF pointB = ScaleToBounds(new PointF(x[i + 1], y[i + 1]));
                    //g.DrawLine(new Pen(new SolidBrush(Color.FromArgb(128, 255, 255, 255)), lineThickness * 5), pointA, pointB);
                    g.FillPolygon(new SolidBrush(plotColor), new PointF[] { pointA, pointB, new PointF(pointB.X, plotHeight - margins), new PointF(pointA.X, plotHeight - margins) });
                }
            }
        }

        public void AreaY(float[] y, Color plotColor, float plotSize, string seriesTitle)
        {
            float[] x = new float[y.Count()];
            for (int i = 0; i < x.Count(); i++)
            {
                x[i] = i / (float)x.Count() * (maxXValue - minXValue) + minXValue;
            }
            AreaXY(x, y, plotColor, plotSize, seriesTitle);
        }

        public void ScatterY(float[] y, Color plotColor, float plotSize, string seriesTitle)
        {
            float[] x = new float[y.Count()];
            for (int i = 0; i < x.Count(); i++)
            {
                x[i] = i / (float)x.Count() * (maxXValue - minXValue) + minXValue;
            }
            ScatterXY(x, y, plotColor, plotSize, seriesTitle);
        }

        public void CalculateXYMinMax(float[] x, float[] y)
        {
            float maxYValue = float.MinValue;
            float minYValue = float.MaxValue;
            float maxXValue = float.MinValue;
            float minXValue = float.MaxValue;
            for (int i = 0; i < y.Count(); i++)
            {
                if (y[i] > maxYValue)
                {
                    maxYValue = y[i];
                }
                if (y[i] < minYValue)
                {
                    minYValue = y[i];
                }

                if (x[i] > maxXValue)
                {
                    maxXValue = x[i];
                }
                if (x[i] < minXValue)
                {
                    minXValue = x[i];
                }
            }
            // replacing calculated values with user given values if they are valid
            if (this.maxYValue == float.MinValue)
            {
                this.maxYValue = maxYValue;
            }
            if (this.minYValue == float.MinValue)
            {
                this.minYValue = minYValue;
            }
            if (this.maxXValue == float.MinValue)
            {
                this.maxXValue = maxXValue;
            }
            if (this.minXValue == float.MinValue)
            {
                this.minXValue = minXValue;
            }
        }

        public void CalculateXYMinMax(float[] y)
        {
            float maxYValue = float.MinValue;
            float minYValue = float.MaxValue;
            float maxXValue = y.Count() - 1;
            float minXValue = 0;
            for (int i = 0; i < y.Count(); i++)
            {
                if (y[i] > maxYValue)
                {
                    maxYValue = y[i];
                }
                if (y[i] < minYValue)
                {
                    minYValue = y[i];
                }
            }
            // replacing calculated values with user given values if they are valid
            if (this.maxYValue != default(float))
            {
                maxYValue = this.maxYValue;
            }
            if (this.minYValue != default(float))
            {
                minYValue = this.minYValue;
            }
            if (this.maxXValue != default(float))
            {
                maxXValue = this.maxXValue;
            }
            if (this.minXValue != default(float))
            {
                minXValue = this.minXValue;
            }
        }

        public void ScatterXY(float[] x, float[] y, Color plotColor, float plotSize, string seriesTitle)
        {
            legend.Add(new LegendEntry(LegendEntry.PlotTypes.FillEllipse, seriesTitle, plotColor, plotSize));
            //plotting
            using (Graphics g = Graphics.FromImage(plot))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // drawing data points
                for (int i = 0; i < x.Count() && i < y.Count(); i++)
                {
                    float xLoc = margins + x[i] / maxXValue * (plotWidth - (margins * 2)) - plotSize / 2;
                    float yLoc = plotHeight - margins - y[i] / maxYValue * (plotHeight - (margins * 2) * maxVerticalScale) - plotSize / 2;

                    if(xLoc > margins && xLoc < plotWidth - margins && yLoc > margins && yLoc < plotHeight - margins)
                    {
                        g.FillEllipse(
                        //new Pen(new SolidBrush(plotColor)),
                        new SolidBrush(plotColor),
                        new RectangleF(
                            margins + x[i] / maxXValue * (plotWidth - (margins * 2)) - plotSize / 2,
                            plotHeight - margins - y[i] / maxYValue * (plotHeight - (margins * 2) * maxVerticalScale) - plotSize / 2,
                            plotSize,
                            plotSize));
                    }
                }
            }
        }

        public void LineXY(float[] x, float[] y, Color plotColor, float lineThickness, string seriesTitle)
        {
            legend.Add(new LegendEntry(LegendEntry.PlotTypes.DrawLine, seriesTitle, plotColor, lineThickness));

            using (Graphics g = Graphics.FromImage(plot))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                for(int i = 0; i < x.Count()  - 1 && i < y.Count() - 1; i++)
                {
                    PointF pointA = ScaleToBounds(new PointF(x[i], y[i]));
                    PointF pointB = ScaleToBounds(new PointF(x[i + 1], y[i + 1]));
                    g.DrawLine(new Pen(new SolidBrush(Color.FromArgb(128, 255, 255, 255)), lineThickness * 5), pointA, pointB);
                    g.DrawLine(new Pen(new SolidBrush(plotColor), lineThickness), pointA, pointB);
                }
            }
        }

        public void LineY(float[] y, Color plotColor, float lineThickness, string seriesTitle)
        {
            float[] x = new float[y.Count()];
            for(int i = 0; i < x.Count(); i++)
            {
                x[i] = i / (float)x.Count() * (maxXValue - minXValue) + minXValue;
            }
            LineXY(x, y, plotColor, lineThickness, seriesTitle);
        }

        private PointF ScaleToBounds (PointF point)
        {
            /*PointF scaled = */return new PointF(
                margins + (point.X - minXValue) / maxXValue * (plotWidth - margins * 2), 
                plotHeight - margins - (point.Y - minYValue) / maxYValue * (plotHeight - margins * 2));
            /*if (scaled.X > plotWidth - margins || scaled.X < margins || scaled.Y < margins || scaled.Y > plotWidth - margins)
            {
                return new PointF(-1, -1);
            }
            return scaled;*/
        }

        public void DrawOutsideGraph ()
        {
            /// draws all the boxes, keys, labels, titles, etc around the graph, but not the graph itself.

            /* layout rules:
             * 
             * margins are 1/20 * (plotHeight + plotWidth)
             * 
             * title is 1/2 graphMargins from the top edge
             * title font size is 1 / 6 * margins
             * title is centered on the x axis
             * 
             * axis label font size is 1 / 11 * margins
            */

            using (Graphics g = Graphics.FromImage(plot))
            {

                // whiting out edge
                g.FillRectangle(
                        new SolidBrush(Color.White),
                        new RectangleF(0, 0, plotWidth, margins));
                g.FillRectangle(
                        new SolidBrush(Color.White),
                        new RectangleF(0, plotHeight - margins, plotWidth, margins));
                g.FillRectangle(
                        new SolidBrush(Color.White),
                        new RectangleF(0, 0, margins, plotHeight));
                g.FillRectangle(
                        new SolidBrush(Color.White),
                        new RectangleF(extendedPlotWidth - margins * rightMarginSizeMultiplier, 0, margins * rightMarginSizeMultiplier, plotHeight));


                StringFormat centredStringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                float titleFontSize = margins / 6;
                Font axisLabelFont = new Font(FontFamily.GenericMonospace, margins / 11);

                // drawing divisions
                if (minXValue != float.MinValue && minYValue != float.MinValue && maxXValue != float.MinValue && maxYValue != float.MinValue)
                {
                    // drawing graph reference lines

                    // x divisions
                    float xDivWidthData = (maxXValue - minXValue) / xDivCount;
                    float xDivWidthPix = xDivWidthData / (maxXValue - minXValue) * (plotWidth - 2 * margins);
                    for (int i = 0; xDivWidthPix * i < plotWidth - margins * 2; i++)
                    {
                        g.DrawLine(new Pen(new SolidBrush(Color.FromArgb(64, 0, 0, 0))),
                            xDivWidthPix * i + margins,
                            margins,
                            xDivWidthPix * i + margins,
                            plotHeight - margins);
                        g.DrawString(
                            "" + RoundToSignificantDigits(maxXValue - xDivWidthData * i, 4),
                            new Font(FontFamily.GenericMonospace,
                            margins / 11),
                            new SolidBrush(Color.Black),
                            xDivWidthPix * i + margins,
                            plotHeight - margins + margins / 11,
                            centredStringFormat);
                    }

                    // y divisions
                    centredStringFormat.Alignment = StringAlignment.Far;
                    float yDivWidthData = (maxYValue - minYValue) / yDivCount;
                    float yDivWidthPix = (yDivWidthData / (maxYValue - minYValue) * (plotHeight - 2 * margins));
                    for (int i = 0; yDivWidthPix * (i - 1) < plotHeight - margins * 2; i++)
                    {
                        g.DrawLine(
                            new Pen(new SolidBrush(Color.FromArgb(64, 0, 0, 0))),
                            margins,
                            plotHeight - (yDivWidthPix * i + margins),
                            plotWidth - margins,
                            plotHeight - (yDivWidthPix * i + margins));
                        g.DrawString(
                            "" + RoundToSignificantDigits(yDivWidthData * i, 4),
                            new Font(FontFamily.GenericMonospace, margins / 11),
                            new SolidBrush(Color.Black),
                            (int)(margins * 0.98f),
                            plotHeight - (yDivWidthPix * i + margins),
                            centredStringFormat);
                    }
                    centredStringFormat.Alignment = StringAlignment.Center;
                }

                // drawing graph region bounds
                g.DrawRectangle(new Pen(new SolidBrush(Color.Black), 1), margins, margins, plotWidth - 2 * margins, plotHeight - 2 * margins);

                // drawing title
                if(title.Length > 75) // if the title is long, center from extendedPlotWidth instead of plotWidth
                {
                    g.DrawString(
                    title,
                    new Font(FontFamily.GenericMonospace, titleFontSize),
                    new SolidBrush(Color.Black),
                    new PointF(extendedPlotWidth / 2, margins / 2),
                    centredStringFormat);
                }
                else
                {
                    g.DrawString(
                    title,
                    new Font(FontFamily.GenericMonospace, titleFontSize),
                    new SolidBrush(Color.Black),
                    new PointF(plotWidth / 2, margins / 2),
                    centredStringFormat);
                }
                

                // drawing x axis label
                g.DrawString(
                    xLabel,
                    axisLabelFont,
                    new SolidBrush(Color.Black),
                    new PointF(plotWidth / 2, plotHeight - margins / 2),
                    centredStringFormat);

                g.RotateTransform(-90);
                // drawing y axis label
                g.DrawString(
                    yLabel,
                    axisLabelFont,
                    new SolidBrush(Color.Black),
                    new PointF(-plotHeight / 2, margins / 4),
                    centredStringFormat);
                g.RotateTransform(90);

                // drawing Legend

                Font legendFont = new Font(FontFamily.GenericMonospace, margins / 11);
                RectangleF legendBounds = new RectangleF(
                    extendedPlotWidth - margins * rightMarginSizeMultiplier * 0.9f, 
                    (plotHeight / 2) - ((legend.Count() + 1) * legendFont.Size * 2 / 2 ),
                    margins * rightMarginSizeMultiplier * 0.8f, 
                    (legend.Count() + 1) * legendFont.Size * 2);
                g.DrawRectangle(new Pen(new SolidBrush(Color.Black), 1), new Rectangle ((int)legendBounds.Location.X, (int)legendBounds.Location.Y, (int)legendBounds.Width, (int)legendBounds.Height));
                g.DrawString("Legend:", legendFont, new SolidBrush(Color.Black), legendBounds.X + legendBounds.Width / 2, legendBounds.Y + legendFont.Size, centredStringFormat);
                StringFormat leftJustifiedStringFormat = new StringFormat();
                leftJustifiedStringFormat.LineAlignment = StringAlignment.Center;
                leftJustifiedStringFormat.Alignment = StringAlignment.Near;
                for(int i = 0; i < legend.Count(); i++)
                {
                    Point centerLegendIcon = new Point(
                        (int)(legendBounds.X + legendFont.Size),
                        (int)(legendBounds.Y + legendFont.Size * (i + 1) * 2 + legendFont.Size));

                    switch (legend[i].plotType)
                    {
                        case LegendEntry.PlotTypes.DrawEllipse:
                            break;
                        case LegendEntry.PlotTypes.FillEllipse:
                            g.FillEllipse(new SolidBrush(legend[i].color), centerLegendIcon.X - legend[i].plotSize / 2, centerLegendIcon.Y - legend[i].plotSize / 2, legend[i].plotSize, legend[i].plotSize);
                            break;
                        case LegendEntry.PlotTypes.DrawLine:
                            g.DrawLine(new Pen(new SolidBrush(legend[i].color), legend[i].plotSize), centerLegendIcon.X - legendFont.Size / 2, centerLegendIcon.Y, centerLegendIcon.X + legendFont.Size / 2, centerLegendIcon.Y);
                            break;
                        case LegendEntry.PlotTypes.FillRectangle:
                            g.FillRectangle(new SolidBrush(legend[i].color), new Rectangle((int)(centerLegendIcon.X -legendFont.Size / 2), (int)(centerLegendIcon.Y - legendFont.Size / 2), (int)(legendFont.Size), (int)(legendFont.Size)));
                            break;
                    }
                    g.DrawString(legend[i].title, legendFont, new SolidBrush(Color.Black), legendBounds.X + legendFont.Size * 2, legendBounds.Y + legendFont.Size * (i + 1) * 2 + legendFont.Size, leftJustifiedStringFormat);
                }
            }
        }

        private static double RoundToSignificantDigits(double d, int digits)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
        }
    }
}