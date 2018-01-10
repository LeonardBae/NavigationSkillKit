using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Azure.Devices;
using System.Collections.Generic;
using System.Text;

namespace mapcontrolbot.Dialogs
{
    [LuisModel("====insert your app id====", "====insert your subscription id====", domain: "====insert your domain====")]
    [Serializable]
    public class RootDialog : LuisDialog<object>
    {
        private const string EntityStatus = "Place";

        private string lightStatus;

        static string connectionString = "====insert your iot hub connection string====";
        static ServiceClient serviceClient;
        private static string DeviceName = "====insert your device name====";

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            var response = context.MakeMessage();
            response.Text = $"Sorry, I did not understand '{result.Query}'. Use 'help' if you need assistance.";
            response.Speak = $"Sorry, I did not understand '{result.Query}'. Say 'help' if you need assistance.";
            response.InputHint = InputHints.ExpectingInput; //chris InputHints.AcceptingInput;

            await context.PostAsync(response);

            context.Wait(this.MessageReceived);
        }

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            //chris 
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            // Check for empty query
            var message = await item;
            if (message.Text == null)
            {
                // Return the Help/Welcome
                await Help(context, null);
            }
            else
            {
                await base.MessageReceived(context, item);
            }
        }

        //chris 
        [LuisIntent("DirectionToPlace")]
        public async Task Control(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            bool isWelcomeDone, firstCommand = false;
            context.ConversationData.TryGetValue<bool>("WelcomeDone", out isWelcomeDone);
            context.ConversationData.TryGetValue<bool>("FirstCommand", out firstCommand);

            // Did we already do this? Has the user followed up an initial query with another one?
            if (!isWelcomeDone)
            {
                var response = context.MakeMessage();
                // For display text, use Summary to display large font, italics - this is to emphasize this
                // is the Skill speaking, not Cortana
                // Continue the displayed text using the Text property of the response message
                response.Summary = $"Welcome to the NeoFalcon Navigation!";
                response.Text = $"We are analyzing your message: '{(await activity).Text}'...";
                // Speak is what is spoken out
                response.Speak = @"<speak version=""1.0"" xml:lang=""en-US"">Welcome to the Neo Falcon navigation<break time=""1000ms""/></speak>"; ;
                // InputHint influences how the microphone behaves
                response.InputHint = InputHints.IgnoringInput;
                // Post the response message
                await context.PostAsync(response);

                // Set a flag in conversation data to record that we already sent out the Welcome message
                context.ConversationData.SetValue<bool>("WelcomeDone", true);
            }

            //9/21
            //var AccessaryQuery = new AccessaryQuery();

            EntityRecommendation lightstatus;

            if (result.TryFindEntity(EntityStatus, out lightstatus))
            {
                lightstatus.Type = "Place";
                lightStatus = lightstatus.Entity.ToLowerInvariant();
            }


            var descriptions = new List<string>()
            { "Drive", "Walk"};
            var speakText = new StringBuilder();


            for (int count = 1; count < 3; count++)
            {
                speakText.Append($"{count}: {descriptions[count - 1]}");
            }

            if (!firstCommand)
            {
                var resultsMessage = context.MakeMessage();
                //resultsMessage.Speak = speakText.ToString();
                resultsMessage.Speak = @"<speak version=""1.0"" xml:lang=""en-US"">There are two transportation available on Neofalcon , "
                    + @" <break time=""1000ms""/> 'first one is Drive'"
                    + @" <break time=""300ms""/> 'second one is Walk '</speak>";
                resultsMessage.InputHint = InputHints.IgnoringInput;
                await context.PostAsync(resultsMessage);

                context.ConversationData.SetValue<bool>("FirstCommand", true);
            }
            else
            {
                var resultsMessage = context.MakeMessage();
                resultsMessage.Speak = @"<speak version=""1.0"" xml:lang=""en-US"">Drive, "
                    + @" <break time=""1000ms""/> Walk  </speak>";

                resultsMessage.InputHint = InputHints.IgnoringInput;
                await context.PostAsync(resultsMessage);
            }


            var choices = new Dictionary<string, IReadOnlyList<string>>()
             {
                { "1", new List<string> { "one", "Drive", ("Drive").ToLowerInvariant() } },
                { "2", new List<string> { "two", "Walk", ("Walk").ToLowerInvariant() } }
            };

            var promptOptions = new PromptOptionsWithSynonyms<string>(
                prompt: "notused", // prompt is not spoken
                choices: choices,
                descriptions: descriptions,
                speak: SSMLHelper.Speak($"Which one do you want to transport?"));

            PromptDialog.Choice(context, LightChoiceReceivedAsync, promptOptions);

        }

        private async Task LightChoiceReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            int choiceIndex = 0;
            int.TryParse(await result, out choiceIndex);

            var speakText = new StringBuilder();

            if (choiceIndex == 1)
            {
                if (lightStatus == "station")
                {
                    // accessaryLightOn
                    var commandMessage = new Message(Encoding.ASCII.GetBytes("driveGangnamStation"));
                    await serviceClient.SendAsync(DeviceName, commandMessage);
                    speakText.Append("You can drive to Gangnam Station by the following route.");
                }
                else if (lightStatus == "office")
                {
                    //accessaryLightOff 
                    var commandMessage = new Message(Encoding.ASCII.GetBytes("driveHancomTower"));
                    await serviceClient.SendAsync(DeviceName, commandMessage);
                    speakText.Append("You can drive to Hancom Tower by the following route.");
                }
            }
            else if (choiceIndex == 2)
            {
                if (lightStatus == "station")
                {
                    // PinOutLightOn
                    var commandMessage = new Message(Encoding.ASCII.GetBytes("walkGangnamStation"));
                    await serviceClient.SendAsync(DeviceName, commandMessage);
                    speakText.Append("You can walk to Gangnam Station by the following route.");
                }
                else if (lightStatus == "office")
                {
                    //PinOutLightOff 
                    var commandMessage = new Message(Encoding.ASCII.GetBytes("walkHancomTower"));
                    await serviceClient.SendAsync(DeviceName, commandMessage);
                    speakText.Append("You can walk to Hancom Tower by the following route.");
                }
            }

            var endMessage = context.MakeMessage();
            endMessage.Speak = speakText.ToString(); //"OK working on it now"; //bld.ToString();
            endMessage.InputHint = InputHints.ExpectingInput; // AcceptingInput : We're basically done, but they could ask another query if they wanted
            await context.PostAsync(endMessage);

            context.Wait(this.MessageReceived);
        }



        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            var response = context.MakeMessage();

            //chris 
            response.Summary = "Hi! Try asking me things like 'direction to office', 'how to i get to the station' or 'where is the office?'";
            response.Speak = @"<speak version=""1.0"" xml:lang=""en-US"">Hi! Try asking me things like 'direction to office', "
                + @" <break time=""200ms""/> 'how to i get to the station'"
                + @" <break time=""300ms""/> and if you wanna finish command "
                 + @" <break time=""300ms""/> say Good Bye'</speak>";

            response.InputHint = InputHints.ExpectingInput;

            await context.PostAsync(response);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Goodbye")]
        public async Task Goodbye(IDialogContext context, LuisResult result)
        {
            var goodByeMessage = context.MakeMessage();
            goodByeMessage.Summary = goodByeMessage.Speak = "Thanks for using NeoFalcon Navigation!";
            goodByeMessage.InputHint = InputHints.IgnoringInput;
            await context.PostAsync(goodByeMessage);

            var completeMessage = context.MakeMessage();
            completeMessage.Type = ActivityTypes.EndOfConversation;
            completeMessage.AsEndOfConversationActivity().Code = EndOfConversationCodes.CompletedSuccessfully;

            await context.PostAsync(completeMessage);

            context.Done(default(object));
        }
    }
}