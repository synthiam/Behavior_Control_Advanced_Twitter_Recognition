using System;
using System.Text.RegularExpressions;
using EZ_Builder;
using EZ_Builder.Config.Sub;
using Twitterizer;

namespace Behavior_Control_Advanced_Twitter {

  public partial class FormMain : EZ_Builder.UCForms.FormPluginMaster {

    enum SearchTypeEnum {
      Mentions,
      Tweets
    };

    bool _isRunning = false;
    DateTime _lastTweetDateTime = new DateTime();
    DateTime _lastMentionDateTime = new DateTime();

    public FormMain() {

      InitializeComponent();
    }

    public override void SetConfiguration(PluginV1 cf) {

      EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterScreenName", string.Empty);
      EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterMessage", string.Empty);
      EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterSuccess", false);

      base.SetConfiguration(cf);
    }

    public override object[] GetSupportedControlCommands() {

      return new string[] {
        ControlCommands.GET_LATEST_MENTION,
        ControlCommands.GET_LATEST_TWEET
        };
    }

    public override void SendCommand(string windowCommand, params string[] values) {

      if (windowCommand.Equals(ControlCommands.GET_LATEST_MENTION, StringComparison.InvariantCultureIgnoreCase)) {

        doit(SearchTypeEnum.Mentions);
      } else if (windowCommand.Equals(ControlCommands.GET_LATEST_TWEET, StringComparison.InvariantCultureIgnoreCase)) {

        doit(SearchTypeEnum.Tweets);

      } else {

        EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterScreenName", string.Empty);
        EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterMessage", string.Empty);
        EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterSuccess", false);

        base.SendCommand(windowCommand, values);
      }
    }

    void doit(SearchTypeEnum searchType) {

      if (_isRunning) {

        Invokers.SetAppendText(textBox1, true, 100, "Already running!");

        return;
      }

      _isRunning = true;

      try {

        string twitterUsername = Common.GetRegistryEntry(Constants.RegistryKeys.TwitterScreenName, string.Empty);

        if (twitterUsername == string.Empty) {

          Invokers.SetAppendText(textBox1, true, 100, "Twitter Username must not be blank");

          EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterScreenName", string.Empty);
          EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterMessage", string.Empty);
          EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterSuccess", false);

          return;
        }

        if (!twitterUsername.StartsWith("@"))
          twitterUsername = "@" + twitterUsername;

        Invokers.SetAppendText(textBox1, true, 100, "Polling {0}", twitterUsername);

        OAuthTokens token = new OAuthTokens {
          AccessToken = Common.GetRegistryEntry(Constants.RegistryKeys.TwitterAccessToken, string.Empty),
          AccessTokenSecret = Common.GetRegistryEntry(Constants.RegistryKeys.TwitterAccessTokenSecret, string.Empty),
          ConsumerKey = Constants.TwitterConsumerKey,
          ConsumerSecret = Constants.TwitterConsumerSecret
        };

        TwitterResponse<TwitterStatusCollection> timeline;

        if (searchType == SearchTypeEnum.Tweets)
          timeline = TwitterTimeline.UserTimeline(token, new UserTimelineOptions {
            Count = 10,
            ScreenName = twitterUsername,
            IncludeRetweets = false
          });
        else
          timeline = TwitterTimeline.Mentions(token, new TimelineOptions() {
            Count = 10,
            IncludeRetweets = false
          });

        if (timeline.Result != RequestResult.Success) {

          Invokers.SetAppendText(textBox1, true, 100, timeline.ErrorMessage);

          EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterScreenName", string.Empty);
          EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterMessage", string.Empty);
          EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterSuccess", false);

          return;
        }

        for (int i = 0; i < timeline.ResponseObject.Count; i++) {

          TwitterStatus message = timeline.ResponseObject[i];

          if (searchType == SearchTypeEnum.Tweets) {

            if (message.CreatedDate <= _lastTweetDateTime) {

              EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterScreenName", string.Empty);
              EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterMessage", string.Empty);
              EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterSuccess", false);

              continue;
            }

            _lastTweetDateTime = message.CreatedDate;
          } else {

            if (message.CreatedDate <= _lastMentionDateTime) {

              EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterScreenName", string.Empty);
              EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterMessage", string.Empty);
              EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterSuccess", false);

              continue;
            }

            _lastMentionDateTime = message.CreatedDate;
          }

          // remove the tagged twitter name from the message
          string messageText = Regex.Replace(message.Text, twitterUsername, string.Empty, RegexOptions.IgnoreCase);

          // remove double spaces
          while (messageText.Contains("  "))
            messageText = messageText.Replace("  ", " ");

          // remove start and ending white spaces
          messageText = messageText.Trim();

          Invokers.SetAppendText(textBox1, true, 100, "{0} - {1}", message.User.ScreenName, messageText);

          EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterScreenName", message.User.ScreenName);
          EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterMessage", messageText);
          EZ_Builder.Scripting.VariableManager.SetVariable("$TwitterSuccess", true);

          return;
        }
      } catch (Exception ex) {

        Invokers.SetAppendText(textBox1, true, 100, "Error: {0}", ex.Message);
      } finally {

        Invokers.SetAppendText(textBox1, true, 100, "Done");

        _isRunning = false;
      }
    }
  }
}
