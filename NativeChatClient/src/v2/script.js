(function ($) {
  // Thanks to BrunoLM (https://stackoverflow.com/a/3855394)
  $.QueryString = (function (paramsArray) {
    let params = {};

    for (let i = 0; i < paramsArray.length; ++i) {
      let param = paramsArray[i].split("=", 2);

      if (param.length !== 2) continue;

      params[param[0]] = decodeURIComponent(param[1].replace(/\+/g, " "));
    }

    return params;
  })(window.location.search.substr(1).split("&"));

  // // Check if 'v' parameter exists
  // if (!$.QueryString.hasOwnProperty("v")) {
  //   console.log("'v' parameter is not present.");
  //   var currentUrl = window.location.href;
  //   var newUrl = addRandomQueryString(currentUrl);
  //   window.location.href = newUrl;
  // } else {
  //   // Check if 'v' parameter is valid
  //   if (Date.now() - $.QueryString.v > 10000) {
  //     console.log("'v' parameter is not up to date.");
  //     var currentUrl = window.location.href;
  //     var cleanUrl = removeRandomQueryString(currentUrl);
  //     var newUrl = addRandomQueryString(cleanUrl);
  //     window.location.href = newUrl;
  //   }
  // }
})(jQuery);

let config = {};
let credentials = {};
// This flag helps us know when it's safe to start the chat
let hasReceivedConfig = false;
let hasReceivedCredentials = false;

window.chrome.webview.addEventListener('message', event => {
  // The event.data contains the JSON string sent from C#
  const message = event.data; // event.data is already a JS object
  
  // Check the type of the message and handle it
  switch (message.type) {
      case 'config':
          config = message.payload;
          hasReceivedConfig = true;
          console.log("Configuration received:", config);
          break;

      case 'credentials':
          credentials = message.payload;
          hasReceivedCredentials = true;
          console.log("Credentials received:", credentials);
          break;
          
      default:
          console.warn("Received unknown message type:", message.type);
          return;
  }

  // Only connect after both objects have been received
  if (hasReceivedConfig && hasReceivedCredentials) {
      console.log(`All data received. Connecting to channel: ${config.channel}`);
      
      // Apply the settings and connect
      Chat.applySettings(config);
      Chat.connect(config.channel);
      generateTestMessages(10);
  }
});

Chat = {
  info: {
    channel: null,
    connected: false,
    animate: null,
    center: null,
    sms: null,
    showBots: null,
    hideCommands: null,
    hideBadges: null,
    hidePaints: null,
    hideColon: null,
    fade: null,
    size: null,
    height: null,
    weight: null,
    font: null,
    stroke: null,
    shadow: null,
    smallCaps: null,
    invert: null,
    blockedUsers: null,
    nicknameColor: null,
    regex: null,
    emoteScale: null,
    readable: null,
    disableSync: null,
    disablePruning: null,
    yt: null,
    voice: null,
    bigSoloEmotes: null,
    messageImage: null,
    disabledCommands: null,
    scale: null,
    showPronouns: null,
    pronounColorMode: null,
    pronounSingleColor1: null,
    pronounSingleColor2: null,
    pronounCustomColors: null,
    // --- The properties below are not configured from C# ---
    emotes: {},
    badges: {},
    userBadges: {},
    specialBadges: {},
    ffzapBadges: null,
    bttvBadges: null,
    seventvBadges: [],
    seventvPaints: {},
    seventvCheckers: {},
    seventvPersonalEmotes: {},
    seventvNoUsers: {},
    seventvNonSubs: {},
    colors: {},
    chatterinoBadges: null,
    cheers: {},
    lines: [],
    bots: ["streamelements", "streamlabs", "nightbot", "moobot", "fossabot"],
    pronouns: {},
    pronounTypes: {},
  },

  // This function takes the config from C# and applies it
  applySettings: function (cfg) {
    // Directly map properties from the config to the Chat.info object
    for (const key in cfg) {
      if (this.info.hasOwnProperty(key)) {
        this.info[key] = cfg[key];
        console.log(key + " = " + this.info[key]);
      }
    }
    
    // Handle special cases that need parsing
    if (cfg.blockedUsers && typeof cfg.blockedUsers === 'string') {
        this.info.blockedUsers = cfg.blockedUsers.split(',');
    }
    if (cfg.disabledCommands && typeof cfg.disabledCommands === 'string') {
        this.info.disabledCommands = cfg.disabledCommands.split(',');
    }
    if (cfg.regex) {
        this.info.regex = new RegExp(cfg.regex);
    }
    if (cfg.pronounCustomColors && typeof cfg.pronounCustomColors === 'string') {
        try {
            this.info.pronounCustomColors = JSON.parse(cfg.pronounCustomColors);
        } catch (e) {
            console.warn("Failed to parse custom pronoun colors:", e);
            this.info.pronounCustomColors = {};
        }
    }
  },

  loadEmotes: function (channelID) {
    Chat.info.emotes = {};
    // Load BTTV, FFZ and 7TV emotes
    ["emotes/global", "users/twitch/" + encodeURIComponent(channelID)].forEach(
      (endpoint) => {
        $.getJSON(
          addRandomQueryString(
            "https://api.betterttv.net/3/cached/frankerfacez/" + endpoint
          )
        ).done(function (res) {
          res.forEach((emote) => {
            if (emote.images["4x"]) {
              var imageUrl = emote.images["4x"];
              var upscale = false;
            } else {
              var imageUrl = emote.images["2x"] || emote.images["1x"];
              var upscale = true;
            }
            Chat.info.emotes[emote.code] = {
              id: emote.id,
              image: imageUrl,
              upscale: upscale,
            };
          });
        });
      }
    );

    ["emotes/global", "users/twitch/" + encodeURIComponent(channelID)].forEach(
      (endpoint) => {
        $.getJSON(
          addRandomQueryString("https://api.betterttv.net/3/cached/" + endpoint)
        ).done(function (res) {
          if (!Array.isArray(res)) {
            res = res.channelEmotes.concat(res.sharedEmotes);
          }
          res.forEach((emote) => {
            Chat.info.emotes[emote.code] = {
              id: emote.id,
              image: "https://cdn.betterttv.net/emote/" + emote.id + "/3x",
              zeroWidth: [
                "5e76d338d6581c3724c0f0b2",
                "5e76d399d6581c3724c0f0b8",
                "567b5b520e984428652809b6",
                "5849c9a4f52be01a7ee5f79d",
                "567b5c080e984428652809ba",
                "567b5dc00e984428652809bd",
                "58487cc6f52be01a7ee5f205",
                "5849c9c8f52be01a7ee5f79e",
              ].includes(emote.id),
              // "5e76d338d6581c3724c0f0b2" => cvHazmat, "5e76d399d6581c3724c0f0b8" => cvMask, "567b5b520e984428652809b6" => SoSnowy, "5849c9a4f52be01a7ee5f79d" => IceCold, "567b5c080e984428652809ba" => CandyCane, "567b5dc00e984428652809bd" => ReinDeer, "58487cc6f52be01a7ee5f205" => SantaHat, "5849c9c8f52be01a7ee5f79e" => TopHat
            };
          });
        });
      }
    );

    $.getJSON(addRandomQueryString("https://7tv.io/v3/emote-sets/global")).done(
      (res) => {
        res?.emotes?.forEach((emote) => {
          const emoteData = emote.data.host.files.pop();
          var link = `https:${emote.data.host.url}/${emoteData.name}`;
          // if link ends in .gif replace with .webp
          if (link.endsWith(".gif")) link = link.replace(".gif", ".webp")
          Chat.info.emotes[emote.name] = {
            id: emote.id,
            image: link,
            zeroWidth: emote.data.flags == 256,
          };
        });
      }
    );

    $.getJSON(
      addRandomQueryString(
        "https://7tv.io/v3/users/twitch/" + encodeURIComponent(channelID)
      )
    ).done((res) => {
      res?.emote_set?.emotes?.forEach((emote) => {
        const emoteData = emote.data.host.files.pop();
        var link = `https:${emote.data.host.url}/${emoteData.name}`;
        // if link ends in .gif replace with .webp
        if (link.endsWith(".gif")) link = link.replace(".gif", ".webp")
        Chat.info.emotes[emote.name] = {
          id: emote.id,
          image: link,
          zeroWidth: emote.data.flags == 256,
        };
      });
    });
  },

  loadPersonalEmotes: async function (channelID) {
    var subbed = await isUserSubbed(channelID);
    if (!subbed) {
      return;
    }
    const emoteSetIDs = [];
    // var nnysNum = 0;

    try {
      const userResponse = await getPersonalEmoteData(channelID);

      userResponse?.emote_sets?.forEach((emoteSet) => {
        if (emoteSet.flags === 4 || emoteSet.flags === 11) {
          if (!emoteSetIDs.includes(emoteSet.id)) {
            emoteSetIDs.push(emoteSet.id);
          }
        }
      });

      Chat.info.seventvPersonalEmotes[channelID] = {};

      for (let i = 0; i < emoteSetIDs.length; i++) {
        const emoteSetResponse = await $.getJSON(
          addRandomQueryString(
            "https://7tv.io/v3/emote-sets/" + encodeURIComponent(emoteSetIDs[i])
          )
        );

        emoteSetResponse?.emotes?.forEach((emote) => {
          const emoteData = emote.data.host.files.pop();
          var link = `https:${emote.data.host.url}/${emoteData.name}`;
          // if link ends in .gif replace with .webp
          if (link.endsWith(".gif")) link = link.replace(".gif", ".webp")
          const personalEmote = {
            name: emote.name,
            id: emote.id,
            image: link,
            zeroWidth: emote.data.flags == 256,
          };
          // Add personalEmote if not already in Chat.info.seventvPersonalEmotes[channelID]
          if (!Chat.info.seventvPersonalEmotes[channelID][personalEmote.name]) {
            Chat.info.seventvPersonalEmotes[channelID][personalEmote.name] =
              personalEmote;
          }
        });
      }
    } catch (error) {
      // console.error("Error loading personal emotes: ", error);
    }
  },

  loadPronounTypes: function() {
    if (Object.keys(Chat.info.pronounTypes).length === 0) {
      $.getJSON(addRandomQueryString("styles/pronoun_types.json")).done(function(res) {
        res.forEach((pronoun) => {
          Chat.info.pronounTypes[pronoun.name] = pronoun.display;
        });
        // Apply custom pronoun colors after types are loaded
        Chat.applyPronounColors();
      }).fail(function() {
        console.warn("Failed to load pronoun types");
      });
    } else {
      // Types already loaded, just apply colors
      Chat.applyPronounColors();
    }
  },

  applyPronounColors: function() {
    if (!Chat.info.showPronouns || Chat.info.pronounColorMode === "default") {
      return;
    }
    
    let customCSS = '';
    
    if (Chat.info.pronounColorMode === "single") {
      customCSS = `
        .pronoun {
          background: linear-gradient(135deg, ${Chat.info.pronounSingleColor1} 0%, ${Chat.info.pronounSingleColor2} 100%) !important;
        }
      `;
    } else if (Chat.info.pronounColorMode === "custom" && Object.keys(Chat.info.pronounCustomColors).length > 0) {
      Object.keys(Chat.info.pronounCustomColors).forEach(type => {
        const colors = Chat.info.pronounCustomColors[type];
        if (colors && colors.color1 && colors.color2) {
          customCSS += `
            .pronoun.${type} {
              background: linear-gradient(135deg, ${colors.color1} 0%, ${colors.color2} 100%) !important;
            }
          `;
        }
      });
    }
    
    if (customCSS) {
      // Create or update style element for custom pronoun colors
      let styleElement = document.getElementById('custom-pronoun-colors');
      if (!styleElement) {
        styleElement = document.createElement('style');
        styleElement.id = 'custom-pronoun-colors';
        document.head.appendChild(styleElement);
      }
      styleElement.textContent = customCSS;
    }
  },

  getUserPronoun: function(username) {
    if (!Chat.info.showPronouns) {
      return;
    }

    // Return cached pronoun if we have it
    if (Chat.info.pronouns[username]) {
      return Chat.info.pronouns[username];
    }

    // Fetch pronoun from API
    $.getJSON(`https://pronouns.alejo.io/api/users/${encodeURIComponent(username)}`)
      .done(function(res) {
        if (res && res.length > 0 && res[0].pronoun_id) {
          const pronounId = res[0].pronoun_id;
          const displayPronoun = Chat.info.pronounTypes[pronounId];
          if (displayPronoun) {
            Chat.info.pronouns[username] = displayPronoun;
            // Update any existing chat lines for this user
            Chat.updatePronounsForUser(username, displayPronoun);
          }
        } else {
          // Cache empty result to avoid repeated API calls
          Chat.info.pronouns[username] = null;
        }
      })
      .fail(function() {
        // Cache empty result to avoid repeated API calls on failure
        Chat.info.pronouns[username] = null;
      });
  },

  updatePronounsForUser: function(username, pronoun) {
    // Update pronouns in existing chat messages for this user
    const $pronounElements = $(`.chat_line[data-nick="${username}"] .pronoun`);
    $pronounElements.each(function() {
      const $element = $(this);
      $element.text(pronoun);
      
      // Find the pronoun type and apply the corresponding CSS class
      const pronounType = Object.keys(Chat.info.pronounTypes).find(key => 
        Chat.info.pronounTypes[key] === pronoun
      );
      if (pronounType) {
        // Remove any existing pronoun type classes
        $element.removeClass(Object.keys(Chat.info.pronounTypes).join(' '));
        $element.addClass(pronounType);
      }
      
      $element.show();
    });
  },

  load: function (callback) {
    GetTwitchUserID(Chat.info.channel, credentials).done(function (res) {
      let error = false;
      if (res.error) {
        console.log("Error getting user ID: " + res.error);
        Chat.info.id = "1"
        error = true;
      }
      if (res.data.length == 0) {
        console.log("No user found");
        Chat.info.id = "1"
        error = true;
      }

      if (!error) {
        console.log("User ID: " + res.data[0].id);
        Chat.info.channelID = res.data[0].id;
        Chat.loadEmotes(Chat.info.channelID);
        seven_ws(Chat.info.channel);

        client_id = res.client_id;

        // Load channel colors
        TwitchAPI("/chat/color?user_id=" + Chat.info.channelID, credentials).done(
          function (res) {
            res = res.data[0];
            Chat.info.colors[Chat.info.channel] = Chat.getUserColor(Chat.info.channel, res);
          }
        );
        Chat.loadUserPaints(Chat.info.channel, Chat.info.channelID);

        // Load pronouns if enabled
        if (Chat.info.showPronouns) {
          Chat.loadPronounTypes();
        }
      }

      // Load CSS
      let size = sizes[Chat.info.size - 1];
     
      if (typeof Chat.info.font !== 'undefined') {
        const fontValue = String(config.font); // Ensure it's a string
        const fontIndex = parseInt(fontValue, 10);

        // Check if the string was successfully parsed as a number AND is a valid index
        if (!isNaN(fontIndex) && fontIndex >= 0 && fontIndex < fonts.length) {
            // It's a valid index, so get the font name from the array
            Chat.info.font = fontIndex;
            console.log("Chat.info.font = " + fonts[fontIndex]);
            appendCSS("font", fonts[fontIndex]);
        } else {
            // It's not a valid index, so it must be a custom font name
            Chat.info.font = fontValue;
            console.log("Chat.info.font = " + Chat.info.font);
            loadCustomFont(Chat.info.font);
        }
      }
      else {
        console.warn("Chat.info.font is undefined!");
      }
      
      /*
      if (typeof Chat.info.font === "number") {
        font = fonts[Chat.info.font];
        appendCSS("font", font);
      } else {
        loadCustomFont(Chat.info.font);
      }*/

      if (Chat.info.size == 1) {
        Chat.info.seven_scale = 20/14;
      } else if (Chat.info.size == 2) {
        Chat.info.seven_scale = 34/14;
      } else if (Chat.info.size == 3) {
        Chat.info.seven_scale = 48/14;
      }

      let emoteScale = 1;
      if (Chat.info.emoteScale > 1) {
        emoteScale = Chat.info.emoteScale;
      }
      if (emoteScale > 3) {
        emoteScale = 3;
      }

      if (Chat.info.center) {
        Chat.info.animate = false;
        Chat.info.invert = false;
        Chat.info.sms = false;
        appendCSS("variant", "center");
      } 
      
      if (Chat.info.sms) {
        Chat.info.center = false;
        Chat.info.animate = false;
        Chat.info.invert = false;
        Chat.info.shadow = 0;
        Chat.info.stroke = false;
        Chat.info.hidePaints = true;
        Chat.info.disablePruning = true;
        Chat.info.hideColon = false;
        appendCSS("variant", "sms");
      }

      appendCSS("size", size);
      if (emoteScale > 1) {
        appendCSS("emoteScale_" + size, emoteScale);
      }

      if (Chat.info.height) {
        if (Chat.info.height > 4) Chat.info.height = 4
        let height = heights[Chat.info.height];
        appendCSS("height", height);
      }
      if (Chat.info.stroke && Chat.info.stroke > 0) {
        if (Chat.info.stroke > 2) Chat.info.stroke = 2
        let stroke = strokes[Chat.info.stroke - 1];
        appendCSS("stroke", stroke);
      }
      if (Chat.info.weight) {
        // console.log("Weight is "+Chat.info.weight)
        if (Chat.info.weight > 5 && Chat.info.weight < 100) {
          Chat.info.weight = 5;
          let weight = weights[Chat.info.weight - 1];
          appendCSS("weight", weight);
        } else if (Chat.info.weight >= 100) {
            $("#chat_container").css("font-weight", Chat.info.weight);
        } else {
          let weight = weights[Chat.info.weight - 1];
          appendCSS("weight", weight);
        }
      }
      if (Chat.info.shadow && Chat.info.shadow > 0) {
        if (Chat.info.shadow > 3) Chat.info.shadow = 3
        let shadow = shadows[Chat.info.shadow - 1];
        appendCSS("shadow", shadow);
      }
      if (Chat.info.smallCaps) {
        appendCSS("variant", "SmallCaps");
      }
      if (Chat.info.invert) {
        appendCSS("variant", "invert");
      }
      if (Chat.info.scale) {
        // Set CSS variable for scaling
        document.documentElement.style.setProperty('--scale', Chat.info.scale);
        // Update viewport to accommodate scaling
        document.documentElement.style.setProperty('--inv-scale', 1/Chat.info.scale);
      }

      // Load badges
      TwitchAPI("/chat/badges/global", credentials).done(function (res) {
        res?.data.forEach((badge) => {
          badge?.versions.forEach((version) => {
            Chat.info.badges[badge.set_id + ":" + version.id] =
              version.image_url_4x;
          });
        });

        TwitchAPI("/chat/badges?broadcaster_id=" + Chat.info.channelID, credentials).done(
          function (res) {
            res?.data.forEach((badge) => {
              badge?.versions.forEach((version) => {
                Chat.info.badges[badge.set_id + ":" + version.id] =
                  version.image_url_4x;
              });
            });

            // const badgeUrl =
            //   "https://cdn.frankerfacez.com/room-badge/mod/" +
            //   Chat.info.channel +
            //   "/4/rounded";
            // const fallbackBadgeUrl =
            //   "https://static-cdn.jtvnw.net/badges/v1/3267646d-33f0-4b17-b3df-f923a41db1d0/3";

            $.getJSON(
              "https://api.frankerfacez.com/v1/_room/id/" +
              encodeURIComponent(Chat.info.channelID)
            ).done(function (res) {
              const badgeUrl =
                "https://cdn.frankerfacez.com/room-badge/mod/" +
                res.room.id +
                "/4/rounded";
              const fallbackBadgeUrl =
                "https://static-cdn.jtvnw.net/badges/v1/3267646d-33f0-4b17-b3df-f923a41db1d0/3";
              if (res.room.moderator_badge) {
                fetch(badgeUrl)
                  .then((response) => {
                    if (response.status === 404) {
                      Chat.info.badges["moderator:1"] = fallbackBadgeUrl;
                    } else {
                      Chat.info.badges["moderator:1"] = badgeUrl;
                    }
                  })
                  .catch((error) => {
                    console.error("Error fetching the badge URL:", error);
                    Chat.info.badges["moderator:1"] = fallbackBadgeUrl;
                  });
              }
              if (res.room.vip_badge) {
                Chat.info.badges["vip:1"] =
                  "https://cdn.frankerfacez.com/room-badge/vip/" +
                  res.room.id +
                  "/4";
              }
            });
          }
        );
      });

      if (!Chat.info.hideBadges) {
        $.getJSON("https://api.ffzap.com/v1/supporters")
          .done(function (res) {
            Chat.info.ffzapBadges = res;
          })
          .fail(function () {
            Chat.info.ffzapBadges = [];
          });
        $.getJSON("https://api.betterttv.net/3/cached/badges")
          .done(function (res) {
            Chat.info.bttvBadges = res;
          })
          .fail(function () {
            Chat.info.bttvBadges = [];
          });

        /* Deprecated endpoint
                $.getJSON('https://7tv.io/v3/badges?user_identifier=login')
                    .done(function(res) {
                        Chat.info.seventvBadges = res.badges;
                    })
                    .fail(function() {
                        Chat.info.seventvBadges = [];
                    });
                */

        $.getJSON("/api/chatterino-badges")
          .done(function (res) {
            Chat.info.chatterinoBadges = res.badges;
          })
          .fail(function () {
            Chat.info.chatterinoBadges = [];
          });

      }

      // Load cheers images
      TwitchAPI("/bits/cheermotes?broadcaster_id=" + Chat.info.channelID, credentials).done(
        function (res) {
          res = res.data;
          res.forEach((action) => {
            Chat.info.cheers[action.prefix] = {};
            action.tiers.forEach((tier) => {
              Chat.info.cheers[action.prefix][tier.min_bits] = {
                image: tier.images.dark.animated["4"],
                color: tier.color,
              };
            });
          });
        }
      );

      callback(true);
    });
  },

  update: setInterval(function () {
    if (Chat.info.lines.length > 0) {
      var lines = Chat.info.lines.join("");

      if (Chat.info.animate) {
        var $auxDiv = $("<div></div>", { class: "hidden" }).appendTo(
          "#chat_container"
        );
        $auxDiv.append(lines);
        var auxHeight = $auxDiv.height();
        $auxDiv.remove();

        var $animDiv = $("<div></div>");
        if (Chat.info.invert) {
          $("#chat_container").prepend($animDiv);
          $animDiv.animate({ height: auxHeight }, 150, function () {
            $(this).remove();
            $("#chat_container").prepend(lines);
          });
        } else {
          $("#chat_container").append($animDiv);
          $animDiv.animate({ height: auxHeight }, 150, function () {
            $(this).remove();
            $("#chat_container").append(lines);
          });
        }
      } else {
        if (Chat.info.invert) {
          $("#chat_container").prepend(lines);
        } else {
          $("#chat_container").append(lines);
        }
      }
      // if (Chat.info.invert) {
      //   $("#chat_container").prepend(lines);
      // } else {
      //   $("#chat_container").append(lines);
      // }
      Chat.info.lines = [];
      var linesToDelete = $(".chat_line").length - 100;
      if (Chat.info.invert) {
        while (linesToDelete > 0) {
          $(".chat_line").eq(-1).remove();
          linesToDelete--;
        }
      } else {
        while (linesToDelete > 0) {
          $(".chat_line").eq(0).remove();
          linesToDelete--;
        }
      }
    } else if (Chat.info.fade) {
      if (Chat.info.invert) {
        var messageTime = $(".chat_line").eq(-1).data("time");
        if ((Date.now() - messageTime) / 1000 >= Chat.info.fade) {
          $(".chat_line")
            .eq(-1)
            .fadeOut(function () {
              $(this).remove();
            });
        }
      } else {
        var messageTime = $(".chat_line").eq(0).data("time");
        if ((Date.now() - messageTime) / 1000 >= Chat.info.fade) {
          if (Chat.info.sms) {
            // Store a reference to the specific element we want to remove
            var $elementToRemove = $(".chat_line").eq(0);
            // we first need to add the .fading-out class to the chat_line
            $elementToRemove.addClass("fading-out");
            // then we need to wait for the animation to finish
            setTimeout(function () {
              // then we can remove the chat_line
              $elementToRemove.remove();
            }, 700);
          } else {
            var $elementToRemove = $(".chat_line").eq(0);
            $elementToRemove
              .eq(0)
              .fadeOut(function () {
                $(this).remove();
              });
          }
        }
      }
    }
  }, 200),

  getRandomColor: function (twitchColors, userId, nick) {
    let colorSeed = parseInt(userId);
    try {
      // Check if the userId was successfully parsed as an integer
      if (isNaN(colorSeed)) {
        // If not a number, sum the Unicode values of all characters in userId string
        colorSeed = 0;
        userId = String(userId); // Ensure userId is a string
        for (let i = 0; i < userId.length; i++) {
          colorSeed += userId.charCodeAt(i);
        }
      }

      // Calculate color index using modulus
      const colorIndex = colorSeed % twitchColors.length;
      return twitchColors[colorIndex];
    } catch (error) {
      console.error("Error parsing userId:", error)
      colorSeed = nick.charCodeAt(0); // Fallback to 1st char of nick if userId parsing fails

      // Calculate color index using modulus
      const colorIndex = colorSeed % twitchColors.length;
      return twitchColors[colorIndex];
    }
  },

  getUserColor: function (nick, info) {
    const twitchColors = [
      "#FF0000", // Red
      "#0000FF", // Blue
      "#008000", // Green
      "#B22222", // Fire Brick
      "#FF7F50", // Coral
      "#9ACD32", // Yellow Green
      "#FF4500", // Orange Red
      "#2E8B57", // Sea Green
      "#DAA520", // Golden Rod
      "#D2691E", // Chocolate
      "#5F9EA0", // Cadet Blue
      "#1E90FF", // Dodger Blue
      "#FF69B4", // Hot Pink
      "#8A2BE2", // Blue Violet
      "#00FF7F", // Spring Green
    ];
    if (typeof info.color === "string") {
      var color = info.color;
      if (Chat.info.readable) {
        if (info.color === "#8A2BE2") {
          info.color = "#C797F4";
        }
        if (info.color === "#008000") {
          info.color = "#00FF00";
        }
        if (info.color === "#2420d9") {
          info.color = "#BCBBFC";
        }
        var colorIsReadable = tinycolor.isReadable("#18181b", info.color, {});
        var color = tinycolor(info.color);
        while (!colorIsReadable) {
          color = color.lighten(5);
          colorIsReadable = tinycolor.isReadable("#18181b", color, {});
        }
      } else {
        var color = info.color;
      }
    } else {
      var color = Chat.getRandomColor(twitchColors, info["user-id"], nick);
      // console.log("generated random color for", nick, color);
      // console.log(info);
      // console.log("userId", info["user-id"]);
      if (Chat.info.readable) {
        if (color === "#8A2BE2") {
          color = "#C797F4";
        }
        if (color === "#008000") {
          color = "#00FF00";
        }
        if (color === "#2420d9") {
          color = "#BCBBFC";
        }
        var colorIsReadable = tinycolor.isReadable("#18181b", color, {});
        var color = tinycolor(color);
        while (!colorIsReadable) {
          color = color.lighten(5);
          colorIsReadable = tinycolor.isReadable("#18181b", color, {});
        }
      } else {
        var color = color;
      }
    }
    return color;
  },

  loadUserBadges: function (nick, userId) {
    Chat.info.userBadges[nick] = [];
    Chat.info.specialBadges[nick] = [];
    
    $.getJSON("https://api.frankerfacez.com/v1/user/" + nick).always(function (
      res
    ) {
      if (res.badges) {
        Object.entries(res.badges).forEach((badge) => {
          var userBadge = {
            description: badge[1].title,
            url: badge[1].urls["4"],
            color: badge[1].color,
          };
          if (!Chat.info.userBadges[nick].includes(userBadge))
            Chat.info.userBadges[nick].push(userBadge);
        });
      }
      Chat.info.ffzapBadges.forEach((user) => {
        if (user.id.toString() === userId) {
          var color = "#755000";
          if (user.tier == 2) color = user.badge_color || "#755000";
          else if (user.tier == 3) {
            if (user.badge_is_colored == 0)
              color = user.badge_color || "#755000";
            else color = false;
          }
          var userBadge = {
            description: "FFZ:AP Badge",
            url: "https://api.ffzap.com/v1/user/badge/" + userId + "/3",
            color: color,
          };
          if (!Chat.info.userBadges[nick].includes(userBadge))
            Chat.info.userBadges[nick].push(userBadge);
        }
      });
      Chat.info.bttvBadges.forEach((user) => {
        if (user.name === nick) {
          var userBadge = {
            description: user.badge.description,
            url: user.badge.svg,
          };
          if (!Chat.info.userBadges[nick].includes(userBadge))
            Chat.info.userBadges[nick].push(userBadge);
        }
      });
      // 7tv functions Added at the end of the file
      (async () => {
        try {
          var sevenInfo = await getUserBadgeAndPaintInfo(userId);
          var seventvBadgeInfo = sevenInfo.badge;

          if (seventvBadgeInfo) {
            var userBadge = {
              description: seventvBadgeInfo.tooltip,
              url: "https://cdn.7tv.app/badge/" + seventvBadgeInfo.id + "/3x",
            };

            if (!Chat.info.userBadges[nick].includes(userBadge)) {
              Chat.info.userBadges[nick] = [];
              Chat.info.userBadges[nick].push(userBadge);
            }
          } else {
            // console.log("No 7tv badge info found for", userId);
          }
        } catch (error) {
          // console.error("Error fetching badge info:", error);
        }
      })();
      // Chat.info.seventvBadges.forEach(badge => {
      //     badge.users.forEach(user => {
      //         if (user === nick) {
      //             var userBadge = {
      //                 description: badge.tooltip,
      //                 url: badge.urls[2][1]
      //             };
      //             if (!Chat.info.userBadges[nick].includes(userBadge)) Chat.info.userBadges[nick].push(userBadge);
      //         }
      //     });
      // });
      Chat.info.chatterinoBadges.forEach((badge) => {
        badge.users.forEach((user) => {
          if (user === userId) {
            var userBadge = {
              description: badge.tooltip,
              url: badge.image3 || badge.image2 || badge.image1,
            };
            if (!Chat.info.userBadges[nick].includes(userBadge))
              Chat.info.userBadges[nick].push(userBadge);
          }
        });
      });
    });
  },

  loadUserPaints: function (nick, userId) {
    // 7tv functions Added at the end of the file
    (async () => {
      try {
        var sevenInfo = await getUserBadgeAndPaintInfo(userId);
        var seventvPaintInfo = sevenInfo.paint;

        if (seventvPaintInfo) {
          if (!Chat.info.seventvPaints[nick]) {
            Chat.info.seventvPaints[nick] = [];
          }
          if (!seventvPaintInfo.image_url) {
            var gradient = createGradient(
              seventvPaintInfo.angle,
              seventvPaintInfo.stops,
              seventvPaintInfo.function,
              seventvPaintInfo.shape,
              seventvPaintInfo.repeat
            );
            var dropShadows = createDropShadows(seventvPaintInfo.shadows);
            var userPaint = {
              type: "gradient",
              name: seventvPaintInfo.name,
              backgroundImage: gradient,
              filter: dropShadows,
            };
            if (Chat.info.seventvPaints[nick]) {
              if (!Chat.info.seventvPaints[nick].includes(userPaint)) {
                Chat.info.seventvPaints[nick] = [];
                Chat.info.seventvPaints[nick].push(userPaint);
              }
            }
          } else {
            if (seventvPaintInfo.shadows) {
              var dropShadows = createDropShadows(seventvPaintInfo.shadows);
              var userPaint = {
                type: "image",
                name: seventvPaintInfo.name,
                backgroundImage: seventvPaintInfo.image_url,
                filter: dropShadows,
              };
            } else {
              var userPaint = {
                type: "image",
                name: seventvPaintInfo.name,
                backgroundImage: seventvPaintInfo.image_url
              };
            }
            
            if (Chat.info.seventvPaints[nick]) {
              if (!Chat.info.seventvPaints[nick].includes(userPaint)) {
                Chat.info.seventvPaints[nick] = [];
                Chat.info.seventvPaints[nick].push(userPaint);
              }
            }
          }
        } else {
          // console.log("No 7tv paint info found for", userId);
          Chat.info.seventvPaints[nick] = [];
        }
      } catch (error) {
        // console.error("Error fetching paint info:", error);
      }
    })();
  },

  getAlmostWhiteColor: function (color) {
    // Create a tinycolor object from the input color
    const baseColor = tinycolor(color);
    
    // First desaturate the color (to reduce color intensity)
    // Then lighten it significantly (to make it almost white)
    return baseColor
      .desaturate(85)   // Reduce saturation by 85%
      .lighten(80)      // Lighten by 80%
      .toString();      // Convert back to string format
  },

  applySMSTheme: function (chatLine, color) {
    // Convert to jQuery object if it's a DOM element
    const $chatLine = $(chatLine);

    var colorIsReadable = tinycolor.isReadable("#ffffff", tinycolor(color), {});
    var darkerColor = tinycolor(color);
    while (!colorIsReadable) {
      darkerColor = darkerColor.darken(5);
      colorIsReadable = tinycolor.isReadable("#ffffff", darkerColor, {});
    }
    
    // Get RGB values from the color
    // const userColor = tinycolor(color);
    // Create a lighter version (40% lighter)
    var hsl = tinycolor(color).toHsl();
    if (hsl.s < 0.1) {
      hsl.s = 0;
    } else {
      hsl.s = 50 / 100; // Convert percentage to [0,1] range
    }
    hsl.l = 90 / 100; // Convert percentage to [0,1] range
    var lighterColor = tinycolor(hsl).toString();
    
    // Apply colors directly to elements using jQuery methods
    const $userInfo = $chatLine.find('.user_info');
    const $message = $chatLine.find('.message');
    
    // Set background colors
    $userInfo.css('backgroundColor', darkerColor);
    $message.css('backgroundColor', lighterColor);
    
    // Set the CSS variable using native DOM API for better compatibility
    if ($message.length) {
      $message[0].style.setProperty('--arrow-color', lighterColor);
    }
    
    // split Chat.info.messageImage by commas to get all the possible images and then pick one at random
    if (!Chat.info.messageImage) {
      return $chatLine;
    }
    const messageImages = Chat.info.messageImage.split(',');
    const randomImage = messageImages[Math.floor(Math.random() * messageImages.length)];
    // Add custom image if configured
    if (randomImage && $message.length) {
      // Check if image already exists to avoid duplicates
      if ($message.find('.message-image').length === 0) {
        const $img = $('<img>', {
          src: randomImage,
          class: 'message-image',
          alt: ''
        });
        $message.append($img);
      }
    }
    
    // Return the jQuery object
    return $chatLine;
  },

  write: function (nick, info, message, service) {
    nick = Chat.sanitizeUsername(nick);
    if (info) {
      if (Chat.info.regex) {
        if (doesStringMatchPattern(message, Chat.info)) {
          return;
        }
      }
      var $chatLine = $("<div></div>");
      $chatLine.addClass("chat_line");
      if (Chat.info.animate) {
        $chatLine.addClass("animate");
      }
      $chatLine.attr("data-nick", nick);
      $chatLine.attr("data-time", Date.now());
      $chatLine.attr("data-id", info.id);
      var $userInfo = $("<span></span>");
      $userInfo.addClass("user_info");

      // if (service == "youtube") {
      //     $userInfo.append('<span id="service" style="color:red";>> | </span>')
      // }
      // if (service == "twitch") {
      //     $userInfo.append('<span id="service" style="color:#6441A4;">> | </span>')
      // }

      // Writing badges
      if (!Chat.info.hideBadges) {
        var badges = [];

        // Special Badges
        if (Chat.info.specialBadges[nick]) {
          Chat.info.specialBadges[nick].forEach((badge) => {
            var $badge = $("<img/>");
            $badge.addClass("badge");
            $badge.attr("src", badge.url);
            $userInfo.append($badge);
          });
        }
        // End Special Badges

        if (info["source-room-id"] && info["source-room-id"] != info["room-id"]) {
          // We are in shared chat, and the message didn't originate here, so we need to add the source badge
          if (Chat.info.sharedBadge && Chat.info.sharedID && info["source-room-id"] == Chat.info.sharedID) {
            // We already have the badge URL and it's the correct channel
            var $sourceBadge = $("<img/>");
            $sourceBadge.addClass("badge");
            // Set the badge URL
            $sourceBadge.attr("src", Chat.info.sharedBadge);
              
            // Append the badge to userInfo only after we have the URL
            $userInfo.append($sourceBadge);
          } else {
            // We don't have the badge URL yet, so we need to fetch it for the first time
            TwitchAPI(`/users?id=${info["source-room-id"]}`, credentials).done(
              function (res) {
                sourceBadgeUrl = res.data[0].profile_image_url;
                Chat.info.sharedBadge = sourceBadgeUrl;
                Chat.info.sharedID = info["source-room-id"];
              }
            );
          }
        }

        const priorityBadges = [
          "predictions",
          "admin",
          "global_mod",
          "staff",
          "twitchbot",
          "broadcaster",
          "moderator",
          "youtubemod",
          "vip",
        ];
        if (typeof info.badges === "string") {
          if (info.badges != "") {
            info.badges.split(",").forEach((badge) => {
              badge = badge.split("/");
              var priority = priorityBadges.includes(badge[0]) ? true : false;
              if (badge[0] == "youtubemod") {
                badges.push({
                  description: badge[0],
                  url: "../styles/yt-mod.webp",
                  priority: priority,
                });
              } else {
                badges.push({
                  description: badge[0],
                  url: Chat.info.badges[badge[0] + ":" + badge[1]],
                  priority: priority,
                });
              }
            });
          }
        }
        var $modBadge;
        badges.forEach((badge) => {
          if (badge.priority) {
            var $badge = $("<img/>");
            $badge.addClass("badge");
            $badge.attr("src", badge.url);
            if (badge.description === "moderator") $modBadge = $badge;
            $userInfo.append($badge);
          }
        });
        badges.forEach((badge) => {
          if (!badge.priority) {
            var $badge = $("<img/>");
            $badge.addClass("badge");
            $badge.attr("src", badge.url);
            $userInfo.append($badge);
          }
        });
        if (Chat.info.userBadges[nick]) {
          Chat.info.userBadges[nick].forEach((badge) => {
            var $badge = $("<img/>");
            $badge.addClass("badge");
            if (badge.color) $badge.css("background-color", badge.color);
            if (badge.description === "Bot" && info.mod === "1") {
              $badge.css("background-color", "rgb(0, 173, 3)");
              $modBadge.remove();
            }
            $badge.attr("src", badge.url);
            $userInfo.append($badge);
          });
        }
      }

      // Writing username
      var $username = $("<span></span>");
      $username.addClass("nick");
      color = Chat.getUserColor(nick, info);
      Chat.info.colors[nick] = color;
      $username.css("color", color);
      if (Chat.info.center) {
        $username.css("padding-right", "0.5em");
      }
      $username.html(info["display-name"] ? info["display-name"] : nick); // if display name is set, use that instead of twitch name
      var $usernameCopy = null;
      // check the info for seventv paints and add them to the username
      if (service != "youtube") {
        if (Chat.info.seventvPaints[nick] && Chat.info.seventvPaints[nick].length > 0) {
          // console.log("Found 7tv paints for " + nick);
          $usernameCopy = $username.clone();
          $usernameCopy.css("position", "absolute");
          $usernameCopy.css("color", "transparent");
          $usernameCopy.css("z-index", "-1");
          if (Chat.info.center) {
            $usernameCopy.css("max-width", "29.9%");
            $usernameCopy.css("padding-right", "0.5em");
            $usernameCopy.css("text-overflow", "clip");
          }
          paint = Chat.info.seventvPaints[nick][0];
          if (paint.type === "gradient") {
            $username.css("background-image", paint.backgroundImage);
          } else if (paint.type === "image") {
            $username.css(
              "background-image",
              "url(" + paint.backgroundImage + ")"
            );
            $username.css("background-color", color);
            $username.css("background-position", "center");
          }
          let userShadow = "";
          if (Chat.info.stroke) {
            // console.log("Stroke is " + Chat.info.stroke)
            if (Chat.info.stroke === 1) {
              userShadow = " drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px)"
            } else if (Chat.info.stroke === 2) {
              userShadow = " drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px)"
            }
          }
          // Process paint.filter to handle large blur drop-shadows correctly
          let finalFilter = '';
          if (paint.filter) {
            // Fix the regex to properly capture entire drop-shadow expressions including closing parenthesis
            const dropShadows = paint.filter.match(/drop-shadow\([^)]*\)/g) || [];
            // console.log("Drop shadows: ", dropShadows);
            const smallBlurShadows = [];
            const largeBlurShadows = [];

            // Check if shadows already form a stroke effect
            const hasStrokeEffect = detectStrokeEffect(dropShadows);
            if (hasStrokeEffect) {
              // console.log("Detected existing stroke effect in paint shadows, disabling additional stroke");
              userShadow = "";
            }
            
            // Categorize drop-shadows based on blur radius
            dropShadows.forEach(shadow => {
              // Extract the blur radius (third value in px)
              const blurMatch = shadow.match(/-?\d+(\.\d+)?px\s+-?\d+(\.\d+)?px\s+(\d+(\.\d+)?)px/);
              if (blurMatch && parseFloat(blurMatch[3]) >= 1) {
                // console.log("Shadow is large because of blur radius: ", blurMatch[3]);
                if (!shadow.endsWith("px)")) {
                  shadow = shadow + ")";
                }
                largeBlurShadows.push(shadow);
              } else {
                try {
                  if (!parseFloat(blurMatch[3])) {
                    // console.log("Couldn't parse blur radius: ", blurMatch[3]);
                  }
                } catch (e) {
                  console.log("Error parsing blur radius for blurMatch: ", blurMatch, "Error: ", e);
                }
                try {
                  // console.log("Shadow is small because of blur radius: ", blurMatch[3]);
                } catch (e) {
                  console.log("Error parsing blur radius: ", e);
                }
                if (!shadow.endsWith("px)")) {
                  shadow = shadow + ")";
                }
                smallBlurShadows.push(shadow);
              }
            });

            // console.log("Small blur shadows: ", smallBlurShadows);
            // console.log("Large blur shadows: ", largeBlurShadows);
            
            // Reconstruct filter with the correct order
            // Small blur shadows + mentionShadow + large blur shadows
            
            if (smallBlurShadows.length > 0) {
              finalFilter += smallBlurShadows.join(' ');
            }
            
            if (userShadow) {
              finalFilter += userShadow;
            }
            
            if (largeBlurShadows.length > 0) {
              finalFilter += ' ' + largeBlurShadows.join(' ');
            }
            
            // Debug log to verify the filter string
          } else {
            finalFilter = userShadow;
          }
          // console.log("Applied filter:", finalFilter);
          $username.css("filter", finalFilter);
          $username.addClass("paint");
          if (Chat.info.hidePaints) {
            $username.addClass("nopaint");
          }
          $userInfo.append($usernameCopy);
        } else {
          let userShadow = "";
          if (Chat.info.stroke) {
            if (Chat.info.stroke === 1) {
              userShadow = " drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px)"
            } else if (Chat.info.stroke === 2) {
              userShadow = " drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px)"
            }
          }
          $username.css("filter", userShadow);
        }
      } else {
        let userShadow = "";
          if (Chat.info.stroke) {
            // console.log("Stroke is " + Chat.info.stroke)
            if (Chat.info.stroke === 1) {
              userShadow = " drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px)"
            } else if (Chat.info.stroke === 2) {
              userShadow = " drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px)"
            }
          }
          $username.css("filter", userShadow);
      }

      if (Chat.info.hideColon && !Chat.info.center) {
        $username.addClass("colon")
      }

      $userInfo.append($username);

      // Add pronouns if enabled
      if (Chat.info.showPronouns && service !== "youtube") {
        var $pronoun = $("<span></span>");
        $pronoun.addClass("pronoun");
        
        // Check if we have cached pronouns for this user
        const cachedPronoun = Chat.info.pronouns[nick];
        if (cachedPronoun) {
          $pronoun.text(cachedPronoun);
          // Find the pronoun type and apply the corresponding CSS class
          const pronounType = Object.keys(Chat.info.pronounTypes).find(key => 
            Chat.info.pronounTypes[key] === cachedPronoun
          );
          if (pronounType) {
            $pronoun.addClass(pronounType);
          }
          // If no specific type found, the default gradient from CSS will be used
          $userInfo.append($pronoun);
        } else if (cachedPronoun !== null) {
          // Only fetch if we haven't already tried (null means we tried and failed/empty)
          $pronoun.text(""); // Empty initially
          Chat.getUserPronoun(nick);
        }
      }

      // Updating the 7tv checker
      if (service != "youtube") {
        if (Chat.info.seventvCheckers[info["user-id"]]) {
          // console.log(
          //   Chat.info.seventvCheckers[info["user-id"]].timestamp +
          //     60000 -
          //     Date.now()
          // );
          if (
            Chat.info.seventvCheckers[info["user-id"]].timestamp + 60000 <
            Date.now()
          ) {
            // console.log("7tv checker expired so checking again");
            Chat.loadUserBadges(nick, info["user-id"]);
            Chat.loadUserPaints(nick, info["user-id"]);
            Chat.loadPersonalEmotes(info["user-id"]);
            const data = {
              enabled: true,
              timestamp: Date.now(),
            };
            Chat.info.seventvCheckers[info["user-id"]] = data;
          }
        }
      }

      // Writing message
      var $message = $("<span></span>");
      $message.addClass("message");
      if (/^\x01ACTION.*\x01$/.test(message)) {
        $message.css("color", color);
        message = message
          .replace(/^\x01ACTION/, "")
          .replace(/\x01$/, "")
          .trim();
        $userInfo.append("<span>&nbsp;</span>");
      } else {
        if (!Chat.info.hideColon || Chat.info.center) {
          var $colon = $("<span></span>");
          $colon.addClass("colon");
          $colon.html(" :");
          $colon.css("color", color);
          $userInfo.append($colon);
        }
      }
      $chatLine.append($userInfo);

      // Replacing emotes and cheers
      var replacements = {};
      if (typeof info.emotes === "string") {
        try {
          // Debug log for emote string format
          // console.log("[Emote Debug] Processing emotes string:", info.emotes);
          
          info.emotes.split("/").forEach((emoteData) => {
            try {
              // Debug log for each emote data piece
              // console.log("[Emote Debug] Processing emote data:", emoteData);
              
              // Defensive coding to prevent t[1] undefined error
              var twitchEmote = emoteData.split(":");
              // console.log("[Emote Debug] Split emote data:", twitchEmote);
              
              // Check if we have both parts of the emote data
              if (twitchEmote.length < 2) {
                // console.error("[Emote Debug] Invalid emote data format, missing colon separator:", emoteData);
                return; // Skip this emote
              }
              
              // More defensive coding for the indices
              var indexesData = twitchEmote[1].split(",")[0];
              if (!indexesData) {
                // console.error("[Emote Debug] Invalid emote indices format:", twitchEmote[1]);
                return; // Skip this emote
              }
              
              var indexes = indexesData.split("-");
              if (indexes.length !== 2) {
                // console.error("[Emote Debug] Invalid emote index range format:", indexesData);
                return; // Skip this emote
              }
              
              var emojis = new RegExp("[\u1000-\uFFFF]+", "g");
              var aux = message.replace(emojis, " ");
              
              // Check if indices are within range
              const startIndex = parseInt(indexes[0]);
              const endIndex = parseInt(indexes[1]);
              
              if (isNaN(startIndex) || isNaN(endIndex) || startIndex < 0 || endIndex >= aux.length || startIndex > endIndex) {
                // console.error("[Emote Debug] Invalid index range:", startIndex, endIndex, "for message length:", aux.length);
                return; // Skip this emote
              }
              
              var emoteCode = aux.substr(startIndex, endIndex - startIndex + 1);
              // console.log("[Emote Debug] Successfully extracted emote code:", emoteCode);
              
              replacements[emoteCode] =
                '<img class="emote" src="https://static-cdn.jtvnw.net/emoticons/v2/' +
                twitchEmote[0] +
                '/default/dark/3.0"/>';
            } catch (innerError) {
              console.error("[Emote Debug] Error processing individual emote:", innerError, "Data:", emoteData);
            }
          });
        } catch (error) {
          console.error("[Emote Debug] Critical error in emote processing:", error, "Full emotes string:", info.emotes);
        }
      } else {
        // console.log("[Emote Debug] No emotes to process or emotes is not a string:", typeof info.emotes);
      }

      message = escapeHtml(message);
      const words = message.split(/\s+/);
      const processedWords = words.map(word => {
        let replacedWord = word;
        let isReplaced = false;

        // Check personal emotes if not YouTube
        if (!isReplaced && service !== "youtube" && Chat.info.seventvPersonalEmotes[info["user-id"]]) {
          Object.entries(Chat.info.seventvPersonalEmotes[info["user-id"]]).forEach((emote) => {
            if (word === emote[0]) {
              let replacement;
              if (emote[1].upscale) {
                replacement = `<img class="emote upscale" src="${emote[1].image}"/>`;
              } else if (emote[1].zeroWidth) {
                replacement = `<img class="emote" data-zw="true" src="${emote[1].image}"/>`;
              } else {
                replacement = `<img class="emote" src="${emote[1].image}"/>`;
              }
              replacedWord = replacement;
              isReplaced = true;
            }
          });
        }

        // Check global emotes
        if (!isReplaced) {
          Object.entries(Chat.info.emotes).forEach((emote) => {
            if (word === emote[0]) {
              let replacement;
              if (emote[1].upscale) {
                replacement = `<img class="emote upscale" src="${emote[1].image}"/>`;
              } else if (emote[1].zeroWidth) {
                replacement = `<img class="emote" data-zw="true" src="${emote[1].image}"/>`;
              } else {
                replacement = `<img class="emote" src="${emote[1].image}"/>`;
              }
              replacedWord = replacement;
              isReplaced = true;
            }
          });
        }

        return { word: replacedWord, isReplaced };
      });

      message = processedWords.reduce((acc, curr, index) => {
        if (index === 0) return curr.word;

        if (curr.isReplaced && processedWords[index - 1].isReplaced) {
          return acc + curr.word;
        } else {
          return acc + ' ' + curr.word;
        }
      }, '');

      // message = escapeHtml(message);

      if (service != "youtube") {
        if (info.bits && parseInt(info.bits) > 0) {
          var bits = parseInt(info.bits);
          var parsed = false;
          for (cheerType of Object.entries(Chat.info.cheers)) {
            var regex = new RegExp(cheerType[0] + "\\d+\\s*", "ig");
            if (message.search(regex) > -1) {
              message = message.replace(regex, "");

              if (!parsed) {
                var closest = 1;
                for (cheerTier of Object.keys(cheerType[1])
                  .map(Number)
                  .sort((a, b) => a - b)) {
                  if (bits >= cheerTier) closest = cheerTier;
                  else break;
                }
                message =
                  '<img class="cheer_emote" src="' +
                  cheerType[1][closest].image +
                  '" /><span class="cheer_bits" style="color: ' +
                  cheerType[1][closest].color +
                  ';">' +
                  bits +
                  "</span> " +
                  message;
                parsed = true;
              }
            }
          }
        }
      }

      var replacementKeys = Object.keys(replacements);
      replacementKeys.sort(function (a, b) {
        return b.length - a.length;
      });

      replacementKeys.forEach((replacementKey) => {
        var regex = new RegExp(
          "(" + escapeRegExp(replacementKey) + ")",
          "g"
        );
        message = message.replace(regex, replacements[replacementKey]);
        message = message.replace(/\s+/g, ' ').trim();
        message = message.replace(/>(\s+)</g, '><');
        message = message.replace(/(<img[^>]*class="emote"[^>]*>)\s+(<img[^>]*class="emote"[^>]*>)/g, '$1$2');
      });

      if (service == "youtube") {
        message = "";
        info.runs.forEach((run) => {
          if ('emoji' in run) {
            // This is an EmojiRun
            message += `<img class="emote" src="${run.emoji.image[0].url}">`;
          } else if ('text' in run) {
            // This is a TextRun
            message += run.text;
          } else {
            // Fallback for any unexpected run type
            message += run.toString().replace(/>/g, '&gt;');
          }
        });

        // Object.entries(Chat.info.emotes).forEach((emote) => {
        //   const emoteRegex = new RegExp(`(^|\\s)${escapeRegExp(emote[0])}($|\\s)`, 'g');
        //   if (emoteRegex.test(message)) {
        //     let replacement;
        //     if (emote[1].upscale) {
        //       replacement = `<img class="emote upscale" src="${emote[1].image}"/>`;
        //     } else if (emote[1].zeroWidth) {
        //       replacement = `<img class="emote" data-zw="true" src="${emote[1].image}"/>`;
        //     } else {
        //       replacement = `<img class="emote" src="${emote[1].image}"/>`;
        //     }
        //     replacements[emote[0]] = replacement;
        //   }
        // });

        // var replacementKeys = Object.keys(replacements);
        // replacementKeys.sort(function (a, b) {
        //   return b.length - a.length;
        // });

        // replacementKeys.forEach((replacementKey) => {
        //   var regex = new RegExp(
        //     "(" + escapeRegExp(replacementKey) + ")",
        //     "g"
        //   );
        //   message = message.replace(regex, replacements[replacementKey]);
        //   message = message.replace(/\s+/g, ' ').trim();
        //   message = message.replace(/>(\s+)</g, '><');
        //   message = message.replace(/(<img[^>]*class="emote"[^>]*>)\s+(<img[^>]*class="emote"[^>]*>)/g, '$1$2');
        // });
      }

      message = twemoji.parse(message);
      $message.html(message);

      if (Chat.info.bigSoloEmotes) {
        // Clone the message content for checking
        const $messageClone = $('<div>').html($message.html());
        
        // Remove all emote images
        const emotes = $messageClone.find('img.emote, img.emoji');
        const emoteCount = emotes.length;
        emotes.remove();
        
        // Check if there's any text content left after removing emotes
        const remainingText = $messageClone.text().trim();
        
        // If no text and we have emotes, this is an emote-only message
        if (remainingText === '' && emoteCount > 0) {
          // Add a class to the message for styling
          $message.addClass('emote-only');
          
          // Find all emotes and add the large class
          $message.find('img.emote, img.emoji').addClass('large-emote');
        }
      }

      // Writing zero-width emotes
      var hasZeroWidth = false;
      messageNodes = $message.children();
      messageNodes.each(function (i) {
        if (
          i != 0 &&
          $(this).data("zw") &&
          ($(messageNodes[i - 1]).hasClass("emote") ||
            $(messageNodes[i - 1]).hasClass("emoji"))
        ) {
          hasZeroWidth = true;
          var $container = $("<span></span>");
          $container.addClass("zero-width_container");
          $container.addClass("staging");
          $(this).addClass("zero-width");
          $(this).addClass("staging")
          $(this).before($container);
          $container.append(messageNodes[i - 1], this);
        }
      });
      message = $message.html() + "</span>"
      $message.html($message.html().trim());

      // New: Handle mentions with seventvPaint
      message = message
        .split(" ")
        .map((word) => {
          if (word.startsWith("@")) {
            var username = word.substring(1).toLowerCase().replace("</span>", "");
            // console.log(username);
            // console.log(Chat.info.seventvPaints[username].length);
            var $mention = $(`<span class="mention">${word}</span>`);
            // console.log(Chat.info.seventvPaints);
            if (Chat.info.seventvPaints[username] && Chat.info.seventvPaints[username].length > 0 && !Chat.info.hidePaints) {
              // console.log(`Found paint for ${username}: ${Chat.info.seventvPaints[username]}`);
              // $mentionCopy = $mention.clone();
              // $mentionCopy.css("position", "absolute");
              // $mentionCopy.css("color", "transparent");
              // $mentionCopy.css("z-index", "-1");
              paint = Chat.info.seventvPaints[username][0];
              if (paint.type === "gradient") {
                $mention.css("background-image", paint.backgroundImage);
              } else if (paint.type === "image") {
                $mention.css(
                  "background-image",
                  "url(" + paint.backgroundImage + ")"
                );
                $mention.css("background-color", color);
                $mention.css("background-position", "center");
              }
              let mentionShadow = "";
              if (Chat.info.stroke) {
                if (Chat.info.stroke === 1) {
                  mentionShadow = " drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px)";
                } else if (Chat.info.stroke === 2) {
                  mentionShadow = " drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px) drop-shadow(rgb(0, 0, 0) 0px 0px 0.5px)"
                }
              }
              // Process paint.filter to handle large blur drop-shadows correctly
              let finalFilter = '';
              if (paint.filter) {
                // Fix the regex to properly capture entire drop-shadow expressions including closing parenthesis
                const dropShadows = paint.filter.match(/drop-shadow\([^)]*\)/g) || [];
                const smallBlurShadows = [];
                const largeBlurShadows = [];

                // Check if shadows already form a stroke effect
                const hasStrokeEffect = detectStrokeEffect(dropShadows);
                if (hasStrokeEffect) {
                  // console.log("Detected existing stroke effect in paint shadows, disabling additional stroke");
                  mentionShadow = "";
                }
                
                // Categorize drop-shadows based on blur radius
                dropShadows.forEach(shadow => {
                  // Extract the blur radius (third value in px)
                  const blurMatch = shadow.match(/-?\d+(\.\d+)?px\s+-?\d+(\.\d+)?px\s+(\d+(\.\d+)?)px/);
                  if (blurMatch && parseFloat(blurMatch[3]) >= 1) {
                    if (!shadow.endsWith("px)")) {
                      shadow = shadow + ")";
                    }
                    largeBlurShadows.push(shadow);
                  } else {
                    try {
                      if (!parseFloat(blurMatch[3])) {
                        // console.log("Couldn't parse blur radius: ", blurMatch[3]);
                      }
                    } catch (e) {
                      console.log("Error parsing blur radius for blurMatch: ", blurMatch, "Error: ", e);
                    }
                    try {
                      // console.log("Shadow is small because of blur radius: ", blurMatch[3]);
                    } catch (e) {
                      console.log("Error parsing blur radius: ", e);
                    }
                    if (!shadow.endsWith("px)")) {
                      shadow = shadow + ")";
                    }
                    smallBlurShadows.push(shadow);
                  }
                });
                
                // Reconstruct filter with the correct order
                // Small blur shadows + mentionShadow + large blur shadows
                
                if (smallBlurShadows.length > 0) {
                  finalFilter += smallBlurShadows.join(' ');
                }
                
                if (mentionShadow) {
                  finalFilter += mentionShadow;
                }
                
                if (largeBlurShadows.length > 0) {
                  finalFilter += ' ' + largeBlurShadows.join(' ');
                }
                
                // Debug log to verify the filter string
                // console.log("Applied filter:", finalFilter);
              } else {
                finalFilter = mentionShadow;
              }
              $mention.css("filter", finalFilter);
              $mention.addClass("paint");
              
              var mentionHtml = $mention[0].outerHTML;
              return mentionHtml;
            }
            
            if (Chat.info.colors[username]) {
              $mention.css("color", Chat.info.colors[username]);
              return $mention[0].outerHTML;
            }
          }
          return word;
        })
        .join(" ");

      // Finalize the message HTML
      $message.html(message);

      // Wrap text nodes in .text-content spans
      const wrapTextNodes = function($element) {
        $element.contents().each(function() {
          // If it's a text node and it's not just whitespace
          if (this.nodeType === 3 && this.nodeValue.trim().length > 0) {
            $(this).wrap('<span class="text-content"></span>');
          } 
          // If it's an element node, process its children recursively
          else if (this.nodeType === 1 && !$(this).is('img, .emote, .emoji, .zero-width, .mention, .paint')) {
            wrapTextNodes($(this));
          }
        });
      };
      
      // Apply the text wrapping
      wrapTextNodes($message);

      $chatLine.append($message);
      if (Chat.info.sms) {
        $chatLine = Chat.applySMSTheme($chatLine, color);
      }
      Chat.info.lines.push($chatLine.wrap("<div>").parent().html());
      if (hasZeroWidth) {
        // console.log("DEBUG Message with mentions and emotes before fixZeroWidth:", $message.html());
        setTimeout(function() {
            fixZeroWidthEmotes(info.id);
        }, 500);
      }
    }
  },

  sanitizeUsername: function (username) {
    return username.replace(/\\s$/, '').trim();
  },

  clearChat: function(nick) {
    setTimeout(function() {
        $('.chat_line[data-nick=' + nick + ']').remove();
    }, 200);
  },

  clearWholeChat: function() {
    setTimeout(function() {
        $('.chat_line').remove();
    }, 200);
  },

  clearMessage: function (id) {
    setTimeout(function () {
      $(".chat_line[data-id=" + id + "]").remove();
    }, 100);
  },

  connect: function (channel) {
    Chat.info.channel = channel;
    var title = $(document).prop("title");
    $(document).prop("title", title + Chat.info.channel);

    Chat.load(function () {
      SendInfoText("Starting Cyan Chat");
      console.log("Cyan Chat: Connecting to IRC server...");
      var socket = new ReconnectingWebSocket(
        "wss://irc-ws.chat.twitch.tv",
        "irc",
        { reconnectInterval: 2000 }
      );

      socket.onopen = function () {
        console.log("Cyan Chat: Connected");
        socket.send("PASS blah\r\n");
        socket.send(
          "NICK justinfan" + Math.floor(Math.random() * 99999) + "\r\n"
        );
        socket.send("CAP REQ :twitch.tv/commands twitch.tv/tags\r\n");
        socket.send("JOIN #" + Chat.info.channel + "\r\n");
      };

      socket.onclose = function () {
        console.log("Cyan Chat: Disconnected");
      };

      socket.onmessage = function (data) {
        data.data.split("\r\n").forEach((line) => {
          if (!line) return;
          var message = window.parseIRC(line);
          if (!message.command) return;

          switch (message.command) {
            case "PING":
              socket.send("PONG " + message.params[0]);
              return;
            case "JOIN":
              console.log("Cyan Chat: Joined channel #" + Chat.info.channel);
              if (!Chat.info.connected) {
                Chat.info.connected = true;
                SendInfoText("Connected to " + Chat.info.channel);
              }
              return;
            case "CLEARMSG":
              if (message.tags)
                Chat.clearMessage(message.tags["target-msg-id"]);
              return;
            case "CLEARCHAT":
              console.log(message);
              if (message.params[1]) {
                Chat.clearChat(message.params[1]);
                console.log("Cyan Chat: Clearing chat of " + message.params[1]);
              } else {
                Chat.clearWholeChat();
                console.log("Cyan Chat: Clearing chat...");
              }
              return;
            case "PRIVMSG":
              if (!message.params[1])
                return;
              
              var nick = message.prefix.split("@")[0].split("!")[0];

              // #region COMMANDS

              // #region REFRESH EMOTES
              if (
                (message.params[1].toLowerCase() === "!chat refresh" ||
                  message.params[1].toLowerCase() === "!chatis refresh" ||
                  message.params[1].toLowerCase() === "!refreshoverlay") &&
                typeof message.tags.badges === "string"
              ) {
                var flag = false;
                message.tags.badges.split(",").forEach((badge) => {
                  badge = badge.split("/");
                  if (badge[0] === "moderator" || badge[0] === "broadcaster") {
                    flag = true;
                    return;
                  }
                });
                
                if (flag) {
                  SendInfoText("Refreshing emotes...");
                  Chat.loadEmotes(Chat.info.channelID);
                  console.log("Cyan Chat: Refreshing emotes...");
                  return;
                }
              }
              // #endregion REFRESH EMOTES

              // #region RELOAD CHAT
              if (
                (message.params[1].toLowerCase() === "!chat reload" ||
                  message.params[1].toLowerCase() === "!chatis reload" ||
                  message.params[1].toLowerCase() === "!reloadchat") &&
                typeof message.tags.badges === "string"
              ) {
                var flag = false;
                message.tags.badges.split(",").forEach((badge) => {
                  badge = badge.split("/");
                  if (badge[0] === "moderator" || badge[0] === "broadcaster") {
                    flag = true;
                    return;
                  }
                });
                
                if (flag) {
                  location.reload();
                }
              }
              // #endregion RELOAD CHAT

              // #region RICKROLL
              if (
                (message.params[1].toLowerCase() === "!chat rickroll" ||
                  message.params[1].toLowerCase() === "!chatis rickroll") &&
                typeof message.tags.badges === "string"
              ) {
                if (Chat.info.disabledCommands.includes("rickroll")) return;
                var flag = false;
                message.tags.badges.split(",").forEach((badge) => {
                  badge = badge.split("/");
                  if (badge[0] === "moderator" || badge[0] === "broadcaster") {
                    flag = true;
                    return;
                  }
                });
                
                if (flag) {
                  console.log("Cyan Chat: Rickrolling...");
                  appendMedia("video", "../media/rickroll.webm")
                  return;
                }
              }
              // #endregion RICKROLL

              // #region Video
              if (
                message.params[1].toLowerCase().startsWith("!chat video") || message.params[1].toLowerCase().startsWith("!chatis video") &&
                typeof message.tags.badges === "string"
              ) {
                var flag = false;
                message.tags.badges.split(",").forEach((badge) => {
                  badge = badge.split("/");
                  if (badge[0] === "moderator" || badge[0] === "broadcaster") {
                    flag = true;
                    return;
                  }
                });
                
                if (flag) {
                  const commandPrefix = message.params[1].toLowerCase().startsWith("!chat video") ? "!chat video" : "!chatis video";
                  var fullCommand = message.params[1].slice(commandPrefix.length).trim();
                  findVideoFile(fullCommand).then(result => {
                    if (result) {
                      console.log(`Cyan Chat: Playing ` + result);
                      appendMedia("video", `../media/${result}`)
                    } else {
                      console.log("Video file not found");
                    }
                  });
                  return;
                }
              }
              // #endregion Video

              // #region TTS
              if (
                message.params[1].toLowerCase().startsWith("!chat tts") || message.params[1].toLowerCase().startsWith("!chatis tts") &&
                typeof message.tags.badges === "string"
              ) {
                if (Chat.info.disabledCommands.includes("tts")) return;
                var flag = false;
                message.tags.badges.split(",").forEach((badge) => {
                  badge = badge.split("/");
                  if (badge[0] === "moderator" || badge[0] === "broadcaster") {
                    flag = true;
                    return;
                  }
                });

                if (flag) {
                  const commandPrefix = message.params[1].toLowerCase().startsWith("!chat tts") ? "!chat tts" : "!chatis tts";
                  var fullCommand = message.params[1].slice(commandPrefix.length).trim();

                  const schema = {
                    v: String,
                    voice: String,
                    s: String
                  };

                  const { flags, rest } = parseFlags(fullCommand, schema);

                  var text = rest;
                  var voice = "Brian"; // Default voice

                  const allowedVoices = [
                    "Brian", "Ivy", "Justin", "Russell", "Nicole", "Emma", "Amy", "Joanna",
                    "Salli", "Kimberly", "Kendra", "Joey", "Mizuki", "Chantal", "Mathieu",
                    "Maxim", "Hans", "Raveena", "Tatyana"
                  ];

                  if (Chat.info.voice) {
                    normalizedVoiceConfig = Chat.info.voice.charAt(0).toUpperCase() + Chat.info.voice.slice(1).toLowerCase();
                    // console.log(normalizedVoiceConfig);
                    if (allowedVoices.includes(normalizedVoiceConfig)) {
                      voice = normalizedVoiceConfig;
                    }
                  }

                  // Check for voice in flags
                  const potentialVoice = flags.v || flags.voice || flags.s;
                  if (potentialVoice) {
                    const normalizedVoice = potentialVoice.charAt(0).toUpperCase() + potentialVoice.slice(1).toLowerCase();
                    if (allowedVoices.includes(normalizedVoice)) {
                      voice = normalizedVoice;
                    }
                  }

                  // Use the queue system instead of direct playback
                  queueTTS(text, voice);
                  console.log(`Cyan Chat: Queued TTS Audio ... [Voice: ${voice}]`);
                  return;
                }
              }
              // #endregion TTS

              // #region YouTube Embed
              if (
                message.params[1].toLowerCase().startsWith("!chat ytplay") || message.params[1].toLowerCase().startsWith("!chatis ytplay") &&
                typeof message.tags.badges === "string"
              ) {
                if (Chat.info.disabledCommands.includes("ytplay")) return;
                var flag = false;
                message.tags.badges.split(",").forEach((badge) => {
                  badge = badge.split("/");
                  if (badge[0] === "moderator" || badge[0] === "broadcaster") {
                    flag = true;
                    return;
                  }
                });
                
                if (flag) {
                  // Parse command arguments
                  const commandPrefix = message.params[1].toLowerCase().startsWith("!chat ytplay") ? "!chat ytplay" : "!chatis ytplay";
                  const commandArgs = message.params[1].slice(commandPrefix.length).trim();
                  
                  // Extract URL and parameters using regex
                  const urlMatch = commandArgs.match(/(?:https?:\/\/)?(?:www\.)?(?:youtube\.com|youtu\.be)\/[^\s]+/i);
                  if (!urlMatch) {
                    console.log("Cyan Chat: No valid YouTube URL found in command");
                    return;
                  }
                  
                  const youtubeUrl = urlMatch[0];
                  const remainingText = commandArgs.replace(youtubeUrl, "").trim();
                  
                  // Parse duration and start time parameters (-d for duration, -s for start time)
                  let duration = 5; // Default duration in seconds
                  let startTime = null; // Will be determined from URL or default to 0
                  
                  const durationMatch = remainingText.match(/-d\s+(\d+)/);
                  if (durationMatch && durationMatch[1]) {
                    duration = parseInt(durationMatch[1]);
                  }
                  
                  const startMatch = remainingText.match(/-s\s+(\d+)/);
                  if (startMatch && startMatch[1]) {
                    startTime = parseInt(startMatch[1]);
                  }

                  const forceOnTopMatch = remainingText.match(/-f/);
                  const forceOnTop = forceOnTopMatch ? true : false;
                  
                  // Extract video ID and process timestamp
                  const videoId = extractYoutubeVideoId(youtubeUrl);
                  if (!videoId) {
                    console.log("Cyan Chat: Could not extract YouTube video ID");
                    return;
                  }
                  
                  const timestamp = extractYoutubeTimestamp(youtubeUrl, startTime);
                  
                  console.log(`Cyan Chat: Playing YouTube video ${videoId} starting at ${timestamp}s for ${duration}s ${forceOnTop ? 'on top' : 'behind text'}`);
                  
                  embedYoutubeVideo(videoId, timestamp, duration, forceOnTop);
                  return;
                }
              }
              // #endregion YouTube Embed

              // #region YouTube Stop
              if (
                message.params[1].toLowerCase().startsWith("!chat ytstop") || message.params[1].toLowerCase().startsWith("!chatis ytstop") &&
                typeof message.tags.badges === "string"
              ) {
                if (Chat.info.disabledCommands.includes("ytstop")) return;
                var flag = false;
                message.tags.badges.split(",").forEach((badge) => {
                  badge = badge.split("/");
                  if (badge[0] === "moderator" || badge[0] === "broadcaster") {
                    flag = true;
                    return;
                  }
                });
                
                if (flag) {
                  console.log("Cyan Chat: Stopping YouTube embed");
                  // SendInfoText("Stopping YouTube embed");
                  removeCurrentMedia();
                  return;
                }
              }
              // #endregion YouTube Stop

              // #region Image Display
              if (
                message.params[1].toLowerCase().startsWith("!chat img") || message.params[1].toLowerCase().startsWith("!chatis img") &&
                typeof message.tags.badges === "string"
              ) {
                if (Chat.info.disabledCommands.includes("img")) return;
                var flag = false;
                message.tags.badges.split(",").forEach((badge) => {
                  badge = badge.split("/");
                  if (badge[0] === "moderator" || badge[0] === "broadcaster") {
                    flag = true;
                    return;
                  }
                });
                
                if (flag) {
                  // Parse the command to extract image URL or emote name
                  // Check if the command starts with !chat img or !chatis img
                  const commandPrefix = message.params[1].toLowerCase().startsWith("!chat img") ? "!chat img" : "!chatis img";
                  // Extract the full command after the prefix
                  const fullCommand = message.params[1].slice(commandPrefix.length).trim();
                  
                  // Define the schema for flag parsing
                  const schema = {
                    d: Number,   // Duration in seconds
                    f: Boolean,  // Force on top flag
                    s: Number,   // Alternate flag for duration
                    t: Number,   // Alternate flag for duration
                    o: Number,   // Opacity
                    w: Number,   // Width
                    h: Number,   // Height
                    duration: Number,
                  };
                  
                  // Parse flags and text
                  const { flags, rest } = parseFlags(fullCommand, schema);
                  
                  // Get the image source (URL or emote name)
                  let imageSource = rest.trim();
                  
                  // Check if it's a URL
                  const isURL = /^https?:\/\//i.test(imageSource);
                  
                  // Get duration from flags (default 5 seconds)
                  const duration = flags.d || flags.duration || flags.s || flags.t || 5;
                  
                  // Get force on top flag
                  const forceOnTop = flags.f || false;

                  const opacity = flags.o || 1;
                  if (opacity < 0) opacity = 0;
                  if (opacity > 1) opacity = 1;
                  
                  if (isURL) {
                    // It's a URL, display directly
                    console.log(`Cyan Chat: Displaying image from URL for ${duration}s`);
                    const img = appendMedia("image", imageSource, forceOnTop, opacity);
                    
                    // Auto-remove after duration
                    setTimeout(() => {
                      removeCurrentMedia('image');
                    }, duration * 1000);
                    
                  } else {
                    // First check if the user has personal 7tv emotes
                    if (Chat.info.seventvPersonalEmotes[message.tags["user-id"]]) {
                      let personalEmote = null;
                      Object.entries(Chat.info.seventvPersonalEmotes[message.tags["user-id"]]).forEach((emote) => {
                        if (imageSource === emote[0]) {
                          personalEmote = emote[1];
                        }
                      });
                      if (personalEmote) {
                        console.log(`Cyan Chat: Displaying personal emote "${personalEmote.name}" for ${duration}s`);
                        const img = appendMedia("image", personalEmote.image, forceOnTop, opacity);
                        
                        // Auto-remove after duration
                        setTimeout(() => {
                          removeCurrentMedia('image');
                        }, duration * 1000);
                        
                        return;
                      }
                    }

                    // Check if it's a native Twitch emote
                    let isTwitchEmote = false;
                    let twitchEmoteId = null;
                    
                    if (typeof message.tags.emotes === "string" && message.tags.emotes !== "") {
                      try {
                        // Split the emotes string by /
                        const emoteParts = message.tags.emotes.split("/");
                        
                        // Loop through each emote data
                        for (const emoteData of emoteParts) {
                          // Split by colon to get ID and positions
                          const twitchEmote = emoteData.split(":");
                          
                          // Skip if invalid format
                          if (twitchEmote.length < 2) continue;
                          
                          // Get the first position
                          const indexesData = twitchEmote[1].split(",")[0];
                          if (!indexesData) continue;
                          
                          // Get start and end indexes
                          const indexes = indexesData.split("-");
                          if (indexes.length !== 2) continue;
                          
                          const startIndex = parseInt(indexes[0]);
                          const endIndex = parseInt(indexes[1]);
                          
                          // Get the emote name from the message
                          var emojis = new RegExp("[\u1000-\uFFFF]+", "g");
                          var aux = message.params[1].replace(emojis, " ");
                          
                          // Check if indices are valid
                          if (isNaN(startIndex) || isNaN(endIndex) || 
                            startIndex < 0 || endIndex >= aux.length || 
                            startIndex > endIndex) continue;
                          
                          // Extract the emote code
                          var emoteCode = aux.substr(startIndex, endIndex - startIndex + 1);
                          
                          // Check if it matches the requested emote
                          if (emoteCode.toLowerCase() === imageSource.toLowerCase()) {
                            isTwitchEmote = true;
                            twitchEmoteId = twitchEmote[0];
                            break;
                          }
                        }
                        
                        // If found, display the Twitch emote
                        if (isTwitchEmote && twitchEmoteId) {
                          console.log(`Cyan Chat: Displaying Twitch emote "${imageSource}" for ${duration}s`);
                          const emoteUrl = `https://static-cdn.jtvnw.net/emoticons/v2/${twitchEmoteId}/default/dark/3.0`;
                          const img = appendMedia("image", emoteUrl, forceOnTop, opacity);
                          
                          // Auto-remove after duration
                          setTimeout(() => {
                            removeCurrentMedia('image');
                          }, duration * 1000);
                          
                          return;
                        }
                      } catch (error) {
                        console.error("Error parsing Twitch emotes:", error);
                      }
                    }

                    // Check if it's an emote from the available emotes
                    const emoteFound = Object.entries(Chat.info.emotes).find(
                      ([emoteName]) => emoteName.toLowerCase() === imageSource.toLowerCase()
                    );
                    
                    if (emoteFound) {
                      console.log(`Cyan Chat: Displaying emote "${emoteFound[0]}" for ${duration}s`);
                      const img = appendMedia("image", emoteFound[1].image, forceOnTop, opacity);
                      
                      // Auto-remove after duration
                      setTimeout(() => {
                        removeCurrentMedia('image');
                      }, duration * 1000);
                      
                    } else {
                      console.log(`Cyan Chat: Emote "${imageSource}" not found`);
                    }
                  }
                  return;
                }
              }
              // #endregion Image Display

              // #region Test Messages
              if (
                message.params[1].toLowerCase().startsWith("!chat test") &&
                typeof message.tags.badges === "string"
              ) {
                if (Chat.info.disabledCommands.includes("test")) return;
                var flag = false;
                message.tags.badges.split(",").forEach((badge) => {
                  badge = badge.split("/");
                  if (badge[0] === "moderator" || badge[0] === "broadcaster") {
                    flag = true;
                    return;
                  }
                });
                
                if (flag) {
                  // Parse the command to extract the number of messages to generate
                  const fullCommand = message.params[1].slice("!chat test".length).trim();
                  
                  // Default to 5 messages if not specified
                  let numMessages = 5;
                  
                  // Try to parse the number from the command
                  const numArg = parseInt(fullCommand);
                  if (!isNaN(numArg) && numArg > 0 && numArg <= 50) {
                    numMessages = numArg;
                  }
                  
                  console.log(`Cyan Chat: Generating ${numMessages} test messages...`);
                  
                  // Generate and display the test messages
                  generateTestMessages(numMessages);
                  
                  return;
                }
              }
              // #endregion Test Messages

              // #endregion COMMANDS

              if (Chat.info.hideCommands) {
                if (/^!.+/.test(message.params[1])) return;
              }

              if (!Chat.info.showBots) {
                if (Chat.info.bots.includes(nick)) {
                  Chat.info.colors[nick] = Chat.getUserColor(nick, message.tags);
                  Chat.loadUserPaints(nick, message.tags["user-id"]);
                  return;
                }
              }

              if (Chat.info.blockedUsers) {
                if (Chat.info.blockedUsers.includes(nick)) {
                  // console.log("Cyan Chat: Hiding blocked user message but getting color...'" + nick + "'");
                  Chat.info.colors[nick] = Chat.getUserColor(nick, message.tags);
                  Chat.loadUserPaints(nick, message.tags["user-id"]);
                  return;
                }
              }

              if (!Chat.info.hideBadges) {
                if (
                  Chat.info.bttvBadges &&
                  Chat.info.seventvBadges &&
                  Chat.info.chatterinoBadges &&
                  Chat.info.ffzapBadges &&
                  !Chat.info.userBadges[nick]
                )
                  Chat.loadUserBadges(nick, message.tags["user-id"]);
              }

              if (
                !Chat.info.seventvPersonalEmotes[message.tags["user-id"]] &&
                !Chat.info.seventvNoUsers[message.tags["user-id"]] &&
                !Chat.info.seventvNonSubs[message.tags["user-id"]]
              ) {
                Chat.loadPersonalEmotes(message.tags["user-id"]);
              }

              if (
                !Chat.info.seventvPaints[nick] &&
                !Chat.info.seventvNoUsers[message.tags["user-id"]] &&
                !Chat.info.seventvNonSubs[message.tags["user-id"]]
              ) {
                Chat.loadUserPaints(nick, message.tags["user-id"]);
              }

              Chat.write(nick, message.tags, message.params[1], "twitch");
              return;
          }
        });
      };
    });
  },
};



// Function to generate random test messages with exact Twitch IRC structure
function generateTestMessages(count) {
  console.log("[Test Messages] Starting test message generation");
  try {
    // Sample usernames for test messages
    // Load usernames from the file
    const usernames = [];
    
    // Make a synchronous AJAX request to get the usernames file
    $.ajax({
      url: './styles/usernames.txt',
      async: false,
      dataType: 'text',
      success: function(data) {
        // Split the data by new lines and filter out empty lines
        const lines = data.split('\n').filter(line => line.trim() !== '');
        // Add each line to the usernames array
        lines.forEach(line => {
          const username = line.trim().replace(/[^a-zA-Z0-9_]/g, ''); // Sanitize username
          if (username.length > 0) {
            usernames.push(username);
          }
        });
      },
      error: function(xhr, status, error) {
        console.error("[Test Messages] Error loading usernames:", error);
      }
    });

    console.log(`[Test Messages] Loaded ${usernames.length} usernames`);
    if (usernames.length === 0) {
      console.error("[Test Messages] No usernames found, aborting message generation");
      return;
    }

    // Sample messages to choose from
    const messageTemplates = [
      "Hello chat! How's everyone doing?",
      "This stream is so entertaining!",
      "I can't believe that just happened!",
      "LOL that was hilarious",
      "gg wp",
      "Is this a new emote?",
      "That's awesome!",
      "I'm just lurking while working",
      "First time here, love the stream",
      "Any recommendations for other streams?",
      "This chat widget is so cool",
      "Wait what happened? I was away",
      "Greetings from [country]!",
      "Anyone else having a good day?",
      "Let's go!",
      "Nice play!",
      "That was incredible",
      "Wow, did not expect that"
    ];

    // Create a queue for messages to avoid sending them all at once
    const messageQueue = [];
    const twitchColors = [
      "#FF0000", "#0000FF", "#008000", "#B22222", "#FF7F50", 
      "#9ACD32", "#FF4500", "#2E8B57", "#DAA520", "#D2691E", 
      "#5F9EA0", "#1E90FF", "#FF69B4", "#8A2BE2", "#00FF7F"
    ];

    // Chance settings
    const EMOTE_CHANCE = 0.7;  // 70% chance of adding an emote
    const PAINT_CHANCE = 0.3;  // 30% chance of using 7TV paint
    const EMOTE_ONLY_CHANCE = 0.2; // 20% chance of emote-only message

    // Get available emotes from Chat.info.emotes
    const availableEmotes = Object.keys(Chat.info.emotes);
    // console.log(`[Test Messages] Found ${availableEmotes.length} available emotes`);
    
    // Generate UUID-like message IDs (format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
    function generateMessageId() {
      const pattern = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx';
      return pattern.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
      });
    }
    
    // Create messages with exact Twitch IRC tag structure
    // console.log("[Test Messages] Creating messages with exact Twitch IRC structure");
    
    // Process paint data early to avoid async issues
    $.getJSON("./styles/unique-paint-types.json")
      .done(function(paintData) {
        try {
          // console.log("[Test Messages] Paint data loaded successfully");
          
          if (!paintData || !paintData.data || !paintData.data.cosmetics || !paintData.data.cosmetics.paints) {
            console.error("[Test Messages] Error: Invalid paint data structure", paintData);
            createAndSendMessages([]);
            return;
          }
          
          const availablePaints = paintData.data.cosmetics.paints;
          // console.log(`[Test Messages] Found ${availablePaints.length} available paints`);
          createAndSendMessages(availablePaints);
        } catch (error) {
          console.error("[Test Messages] Error processing paint data:", error);
          createAndSendMessages([]);
        }
      })
      .fail(function(error) {
        console.error("[Test Messages] Failed to load paint data:", error);
        createAndSendMessages([]);
      });
    
    // Create and send test messages
    function createAndSendMessages(availablePaints) {
      // Pronoun types for test users
      const pronounTypes = [
        { display: "He/Him", name: "hehim" },
        { display: "She/Her", name: "sheher" },
        { display: "They/Them", name: "theythem" },
        { display: "She/They", name: "shethem" },
        { display: "He/They", name: "hethem" },
        { display: "He/She", name: "heshe" },
        { display: "Xe/Xem", name: "xexem" },
        { display: "Fae/Faer", name: "faefaer" },
        { display: "Ve/Ver", name: "vever" },
        { display: "Ae/Aer", name: "aeaer" },
        { display: "Zie/Hir", name: "ziehir" },
        { display: "Per/Per", name: "perper" },
        { display: "E/Em", name: "eem" },
        { display: "It/Its", name: "itits" }
      ];
      
      // Create messages
      for (let i = 0; i < count; i++) {
        const username = usernames[Math.floor(Math.random() * usernames.length)] + `${Math.floor(Math.random() * 1000000).toString()}`;
        const userId = (Math.floor(Math.random() * 900000) + 100000).toString();
        const roomId = "123456789";
        
        // Assign random pronouns to test users (only if pronouns are enabled)
        if (Chat.info.showPronouns) {
          // 80% chance of having pronouns (some users might not have them set)
          if (Math.random() < 0.8) {
            const randomPronoun = pronounTypes[Math.floor(Math.random() * pronounTypes.length)];
            Chat.info.pronouns[username] = randomPronoun.display;
            
            // Also ensure the pronoun type mapping exists
            if (!Chat.info.pronounTypes[randomPronoun.name]) {
              Chat.info.pronounTypes[randomPronoun.name] = randomPronoun.display;
            }
            
            // console.log(`[Test Messages] Assigned pronouns "${randomPronoun.display}" to user ${username}`);
          }
        }
        
        // Generate message content
        let emoteOnly = Math.random() < EMOTE_ONLY_CHANCE;
        let textMessage;
        
        if (emoteOnly) {
          // Create an emote-only message with 1-2 random emotes
          const emoteCount = Math.floor(Math.random() * 2) + 1;
          const selectedEmotes = [];
          
          for (let j = 0; j < emoteCount; j++) {
            if (availableEmotes.length > 0) {
              const randomIndex = Math.floor(Math.random() * availableEmotes.length);
              const randomEmote = availableEmotes[randomIndex];
              selectedEmotes.push(randomEmote);
            }
          }
          
          textMessage = selectedEmotes.join(" ");
          console.log(`[Test Messages] Created emote-only message: "${textMessage}"`);
        } else {
          // Start with a base message from templates
          textMessage = messageTemplates[Math.floor(Math.random() * messageTemplates.length)];
          
          // Maybe add a mention
          if (Math.random() < 0.3) {
            const mentionedUser = usernames[Math.floor(Math.random() * usernames.length)] + `${Math.floor(Math.random() * 1000000).toString()}`;
            // Initialize empty paint array for every user to prevent undefined errors
            if (!Chat.info.seventvPaints[mentionedUser]) {
              Chat.info.seventvPaints[mentionedUser] = [];
            }

            // Assign random pronouns to mentioned users too (only if pronouns are enabled)
            if (Chat.info.showPronouns && Math.random() < 0.8) {
              const randomPronoun = pronounTypes[Math.floor(Math.random() * pronounTypes.length)];
              Chat.info.pronouns[mentionedUser] = randomPronoun.display;
              
              // Also ensure the pronoun type mapping exists
              if (!Chat.info.pronounTypes[randomPronoun.name]) {
                Chat.info.pronounTypes[randomPronoun.name] = randomPronoun.display;
              }
            }

            // 30% chance of using a Twitch color, else generate a random hex
            let mentionColor;
            if (Math.random() < 0.3) {
              mentionColor = twitchColors[Math.floor(Math.random() * twitchColors.length)];
            } else {
              // Generate random hex color
              mentionColor = '#' + Math.floor(Math.random() * 16777215).toString(16).padStart(6, '0');
            }
            if (Chat.info.readable) {
              if (mentionColor === "#8A2BE2") {
                mentionColor = "#C797F4";
              }
              if (mentionColor === "#008000") {
                mentionColor = "#00FF00";
              }
              if (mentionColor === "#2420d9") {
                mentionColor = "#BCBBFC";
              }
              var colorIsReadable = tinycolor.isReadable("#18181b", mentionColor, {});
              var readableColor = tinycolor(mentionColor);
              while (!colorIsReadable) {
                readableColor = readableColor.lighten(5);
                colorIsReadable = tinycolor.isReadable("#18181b", readableColor, {});
              }
              mentionColor = readableColor
            }
            Chat.info.colors[mentionedUser] = mentionColor;

            // Apply paint to some mentioned users
            const mentionUsePaint = Math.random() < PAINT_CHANCE && availablePaints.length > 0;
            let mentionHasPaint = false;
            
            if (mentionUsePaint) {
              try {
                // console.log(`[Test Messages] Applying paint to user: ${mentionedUser}`);
                mentionHasPaint = true;
                
                const mentionRandomPaintIndex = Math.floor(Math.random() * availablePaints.length);
                const mentionRandomPaint = availablePaints[mentionRandomPaintIndex];
                
                // Create paint based on type
                if (mentionRandomPaint.function === "URL") {
                  // Image paint
                  let mentionShadows = "";
                  if (mentionRandomPaint.shadows && Array.isArray(mentionRandomPaint.shadows)) {
                    try {
                      mentionShadows = createDropShadows(mentionRandomPaint.shadows);
                    } catch (error) {
                      console.error(`[Test Messages] Error creating shadows: ${error.message}`);
                    }
                  }
                  
                  Chat.info.seventvPaints[mentionedUser] = [{
                    type: "image",
                    name: mentionRandomPaint.name,
                    backgroundImage: mentionRandomPaint.image_url,
                    filter: mentionShadows
                  }];
                } else {
                  // Gradient paint
                  try {
                    if (Array.isArray(mentionRandomPaint.stops) && mentionRandomPaint.stops.length > 0) {
                      const mentionGradient = createGradient(
                        mentionRandomPaint.angle || 0,
                        mentionRandomPaint.stops,
                        mentionRandomPaint.function || "LINEAR_GRADIENT",
                        mentionRandomPaint.shape || "circle",
                        mentionRandomPaint.repeat || false
                      );
                      
                      let mentionShadows = "";
                      if (mentionRandomPaint.shadows && Array.isArray(mentionRandomPaint.shadows)) {
                        mentionShadows = createDropShadows(mentionRandomPaint.shadows);
                      }
                      
                      Chat.info.seventvPaints[mentionedUser] = [{
                        type: "gradient",
                        name: mentionRandomPaint.name,
                        backgroundImage: mentionGradient,
                        filter: mentionShadows
                      }];
                    }
                  } catch (error) {
                    console.error(`[Test Messages] Error creating gradient: ${error.message}`);
                  }
                }
              } catch (error) {
                console.error(`[Test Messages] Error applying paint: ${error.message}`);
              }
            }

            textMessage = textMessage + " @" + mentionedUser;
          }
          
          // console.log(`[Test Messages] Created text message: "${textMessage.substring(0, 30)}${textMessage.length > 30 ? '...' : ''}"`)
        }
        
        // Create a Twitch IRC tag object with ALL required fields
        const isMod = Math.random() < 0.2;
        const isBroadcaster = Math.random() < 0.1;
        const color = Math.random() < 0.3 ? twitchColors[Math.floor(Math.random() * twitchColors.length)] : '#' + Math.floor(Math.random() * 16777215).toString(16).padStart(6, '0');
        
        // Create badges string
        let badgesString = "";
        if (isBroadcaster) {
          badgesString = "broadcaster/1";
        } else if (isMod) {
          badgesString = "moderator/1";
        }
        
        // Complete tags object that exactly matches real Twitch IRC messages
        const tags = {
          "badge-info": "",
          "badges": badgesString,
          "client-nonce": Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15),
          "color": color,
          "display-name": username,
          "emotes": "",
          "first-msg": "0",
          "flags": "",
          "id": generateMessageId(),
          "mod": isMod ? "1" : "0",
          "room-id": roomId,
          "subscriber": "0",
          "tmi-sent-ts": Date.now().toString(),
          "turbo": "0",
          "user-id": userId,
          "user-type": ""
        };
        
        // Initialize empty paint array for every user to prevent undefined errors
        if (!Chat.info.seventvPaints[username]) {
          Chat.info.seventvPaints[username] = [];
        }
        
        // Apply paint to some users
        const usePaint = Math.random() < PAINT_CHANCE && availablePaints.length > 0;
        let hasPaint = false;
        
        if (usePaint) {
          try {
            // console.log(`[Test Messages] Applying paint to user: ${username}`);
            hasPaint = true;
            
            const randomPaintIndex = Math.floor(Math.random() * availablePaints.length);
            const randomPaint = availablePaints[randomPaintIndex];
            
            // Create paint based on type
            if (randomPaint.function === "URL") {
              // Image paint
              let shadows = "";
              if (randomPaint.shadows && Array.isArray(randomPaint.shadows)) {
                try {
                  shadows = createDropShadows(randomPaint.shadows);
                } catch (error) {
                  console.error(`[Test Messages] Error creating shadows: ${error.message}`);
                }
              }
              
              Chat.info.seventvPaints[username] = [{
                type: "image",
                name: randomPaint.name,
                backgroundImage: randomPaint.image_url,
                filter: shadows
              }];
            } else {
              // Gradient paint
              try {
                if (Array.isArray(randomPaint.stops) && randomPaint.stops.length > 0) {
                  const gradient = createGradient(
                    randomPaint.angle || 0,
                    randomPaint.stops,
                    randomPaint.function || "LINEAR_GRADIENT",
                    randomPaint.shape || "circle",
                    randomPaint.repeat || false
                  );
                  
                  let shadows = "";
                  if (randomPaint.shadows && Array.isArray(randomPaint.shadows)) {
                    shadows = createDropShadows(randomPaint.shadows);
                  }
                  
                  Chat.info.seventvPaints[username] = [{
                    type: "gradient",
                    name: randomPaint.name,
                    backgroundImage: gradient,
                    filter: shadows
                  }];
                }
              } catch (error) {
                console.error(`[Test Messages] Error creating gradient: ${error.message}`);
              }
            }
          } catch (error) {
            console.error(`[Test Messages] Error applying paint: ${error.message}`);
          }
        }
        
        // Add message to queue
        messageQueue.push({
          username,
          tags,
          message: textMessage,
          delay: 200 + Math.floor(Math.random() * 300),
          hasPaint: hasPaint
        });
      }
      
      // Send messages with delays
      let cumulativeDelay = 0;
      messageQueue.forEach((item, index) => {
        cumulativeDelay += item.delay;
        setTimeout(() => {
          try {
            // console.log(`[Test Messages] Sending message ${index+1}/${messageQueue.length} (user: ${item.username}, has paint: ${item.hasPaint})`);
            Chat.write(item.username, item.tags, item.message, "twitch");
          } catch (error) {
            console.error(`[Test Messages] Error sending message: ${error.message}`);
            console.error(error.stack);
          }
        }, cumulativeDelay);
      });
      
      // Notify user
      SendInfoText(`Generated ${count} test messages`);
    }
  } catch (error) {
    console.error("[Test Messages] Critical error:", error);
    console.error(error.stack);
    SendInfoText("Error generating test messages");
  }
}

function detectStrokeEffect(dropShadows) {
  // Bail early if there aren't enough shadows to form a stroke
  if (!dropShadows || dropShadows.length < 3) {
    return false;
  }
  
  // Extract shadow directions and properties
  const shadowDirections = new Set();
  let hasMultipleDirections = false;
  let hasOppositeDirections = false;
  let hasBlackColor = false;
  let hasLargeBlur = false;
  
  // Parse each drop shadow to analyze its properties
  dropShadows.forEach(shadow => {
    // Extract x, y, blur and color
    const match = shadow.match(/drop-shadow\(\s*(-?\d+(\.\d+)?)px\s+(-?\d+(\.\d+)?)px\s+(\d+(\.\d+)?)px\s+(.+)\)/);
    
    if (match) {
      const x = parseFloat(match[1]);
      const y = parseFloat(match[3]);
      const blur = parseFloat(match[5]);
      const color = match[7];
      
      // Check for black or very dark color
      if (color.includes('rgba(0, 0, 0,') || color.includes('rgb(0, 0, 0') || 
          color.includes('#000') || color.includes('black')) {
        hasBlackColor = true;
      }
      
      // Add direction to set
      const direction = getDirection(x, y);
      shadowDirections.add(direction);
      
      // Check if there are large blur values, indicating more of a glow than a stroke
      if (blur >= 2) {
        hasLargeBlur = true;
      }
      
      // Check for opposite directions
      if (shadowDirections.has('right') && shadowDirections.has('left')) hasOppositeDirections = true;
      if (shadowDirections.has('up') && shadowDirections.has('down')) hasOppositeDirections = true;
    }
  });
  
  // Check if we have at least 3 directions (enough to form a partial stroke)
  hasMultipleDirections = shadowDirections.size >= 3;
  
  // console.log("Shadow analysis:", {
  //   directions: Array.from(shadowDirections),
  //   hasMultipleDirections,
  //   hasOppositeDirections,
  //   hasBlackColor,
  //   hasLargeBlur
  // });
  
  // Consider it a stroke effect if:
  // 1. It has multiple directions (at least 3)
  // 2. It has some opposite directions (complete surrounding)
  // 3. Uses black/dark color
  // 4. Has reasonable blur values for a stroke effect
  return hasMultipleDirections && hasOppositeDirections && hasBlackColor;
}

// Helper function to categorize shadow direction
function getDirection(x, y) {
  if (x > 0 && Math.abs(x) > Math.abs(y)) return 'right';
  if (x < 0 && Math.abs(x) > Math.abs(y)) return 'left';
  if (y > 0 && Math.abs(y) >= Math.abs(x)) return 'down';
  if (y < 0 && Math.abs(y) >= Math.abs(x)) return 'up';
  return 'center'; // for (0,0) or very small values
}