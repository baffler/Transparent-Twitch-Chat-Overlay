// SIZE

const size_small = `
#example {
  font-size: 20px;
}

#example .badge {
  width: 16px;
  height: 16px;
  margin-right: 2px;
  margin-bottom: 3px;
}

#example .badge:last-of-type {
  margin-right: 3px;
}

#example .colon {
  margin-right: 8px;
}

#example .cheer_bits {
  font-weight: 700;
  margin-right: 4px;
}

#example .cheer_emote {
  max-height: 25px;
  margin-bottom: -6px;
}

#example .emote {
  max-width: 75px;
  height: 25px;
}

#example .emoji {
  height: 22px;
}`;

const size_medium = `
#example {
  font-size: 34px;
}

#example .badge {
  width: 28px;
  height: 28px;
  margin-right: 4px;
  margin-bottom: 6px;
}

#example .badge:last-of-type {
  margin-right: 6px;
}

#example .colon {
  margin-right: 14px;
}

#example .cheer_bits {
  font-weight: 600;
  margin-right: 7px;
}

#example .cheer_emote {
  max-height: 42px;
  margin-bottom: -10px;
}

#example .emote {
  max-width: 128px;
  height: 42px;
}

#example .emoji {
  height: 39px;
}`;

const size_large = `
#example {
  font-size: 48px;
}

#example .badge {
  width: 40px;
  height: 40px;
  margin-right: 5px;
  margin-bottom: 8px;
}

#example .badge:last-of-type {
  margin-right: 8px;
}

#example .colon {
  margin-right: 20px;
}

#example .cheer_bits {
  font-weight: 500;
  margin-right: 10px;
}

#example .cheer_emote {
  max-height: 60px;
  margin-bottom: -15px;
}

#example .emote {
  max-width: 180px;
  height: 60px;
}

#example .emoji {
  height: 55px;
}`;

// FONT WEIGHT

const weight_one = `
#example {
  font-weight: 200;
}
`;

const weight_two = `
#example {
  font-weight: 400;
}
`;

const weight_three = `
#example {
  font-weight: 600;
}
`;

const weight_four = `
#example {
  font-weight: 800;
}
`;

const weight_five = `
#example {
  font-weight: 1000;
}
`;

// EMOTE SCALE

const ES_small_2 = `
#example .emote {
  max-height: 50px;
  max-width: 150px;
  height: 50px;
}

.zero-width_container {
  margin-bottom: 11px;
  margin-top: 11px;
}`;

const ES_small_3 = `
#example .emote {
  max-height: 75px;
  max-width: 225px;
  height: 75px;
}

.zero-width_container {
  margin-bottom: 16.5px;
  margin-top: 16.5px;
}`;

const ES_medium_2 = `
#example .emote {
  max-height: 84px;
  max-width: 252px;
  height: 84px;
}

.zero-width_container {
  margin-bottom: 18.5px;
  margin-top: 18.5px;
}`;

const ES_medium_3 = `
#example .emote {
  max-height: 126px;
  max-width: 378px;
  height: 126px;
}

.zero-width_container {
  margin-bottom: 27.5px;
  margin-top: 27.5px;
}`;

const ES_large_2 = `
#example .emote {
  max-height: 120px;
  max-width: 360px;
  height: 120px;
}

.zero-width_container {
  margin-bottom: 26.25px;
  margin-top: 26.25px;
}`;

const ES_large_3 = `
#example .emote {
  max-height: 180px;
  max-width: 540px;
  height: 180px;
}

.zero-width_container {
  margin-bottom: 28px;
  margin-top: 28px;
}`;

// STROKE

const stroke_fine = `
:root {
  --stroke-1: 1px;
  --stroke-2: 2px;
  --stroke-3: 3px;
  --stroke-1-min: -1px;
  --stroke-2-min: -2px;
  --stroke-3-min: -3px;
}

#example {
  text-shadow: var(--stroke-1) var(--stroke-3) 0px #000,
               var(--stroke-2) var(--stroke-2) 0px #000,
               var(--stroke-3) var(--stroke-1) 0px #000,
               var(--stroke-3) 0px 0px #000,
               var(--stroke-3) var(--stroke-1-min) 0px #000,
               var(--stroke-2) var(--stroke-2-min) 0px #000,
               var(--stroke-1) var(--stroke-3-min) 0px #000,
               var(--stroke-1-min) var(--stroke-3) 0px #000,
               var(--stroke-2-min) var(--stroke-2) 0px #000,
               var(--stroke-3-min) var(--stroke-1) 0px #000,
               var(--stroke-3-min) 0px 0px #000,
               var(--stroke-3-min) var(--stroke-1-min) 0px #000,
               var(--stroke-2-min) var(--stroke-2-min) 0px #000,
               var(--stroke-1-min) var(--stroke-3-min) 0px #000,
               var(--stroke-3) var(--stroke-1) 0px #000,
               var(--stroke-2) var(--stroke-2) 0px #000,
               var(--stroke-1) var(--stroke-3) 0px #000,
               0px var(--stroke-3) 0px #000,
               var(--stroke-1-min) var(--stroke-3) 0px #000,
               var(--stroke-2-min) var(--stroke-2) 0px #000,
               var(--stroke-3-min) var(--stroke-1) 0px #000,
               var(--stroke-3) var(--stroke-1-min) 0px #000,
               var(--stroke-2) var(--stroke-2-min) 0px #000,
               var(--stroke-1) var(--stroke-3-min) 0px #000,
               0px var(--stroke-3-min) 0px #000,
               var(--stroke-1-min) var(--stroke-3-min) 0px #000,
               var(--stroke-2-min) var(--stroke-2-min) 0px #000,
               var(--stroke-3-min) var(--stroke-1-min) 0px #000;
 
}`;

const stroke_thick = `
:root {
  --stroke-1: 1.5px;
  --stroke-2: 3px;
  --stroke-3: 4.5px;
  --stroke-1-min: -1.5px;
  --stroke-2-min: -3px;
  --stroke-3-min: -4.5px;
}

#example {
  text-shadow: var(--stroke-1) var(--stroke-3) 0px #000,
               var(--stroke-2) var(--stroke-2) 0px #000,
               var(--stroke-3) var(--stroke-1) 0px #000,
               var(--stroke-3) 0px 0px #000,
               var(--stroke-3) var(--stroke-1-min) 0px #000,
               var(--stroke-2) var(--stroke-2-min) 0px #000,
               var(--stroke-1) var(--stroke-3-min) 0px #000,
               var(--stroke-1-min) var(--stroke-3) 0px #000,
               var(--stroke-2-min) var(--stroke-2) 0px #000,
               var(--stroke-3-min) var(--stroke-1) 0px #000,
               var(--stroke-3-min) 0px 0px #000,
               var(--stroke-3-min) var(--stroke-1-min) 0px #000,
               var(--stroke-2-min) var(--stroke-2-min) 0px #000,
               var(--stroke-1-min) var(--stroke-3-min) 0px #000,
               var(--stroke-3) var(--stroke-1) 0px #000,
               var(--stroke-2) var(--stroke-2) 0px #000,
               var(--stroke-1) var(--stroke-3) 0px #000,
               0px var(--stroke-3) 0px #000,
               var(--stroke-1-min) var(--stroke-3) 0px #000,
               var(--stroke-2-min) var(--stroke-2) 0px #000,
               var(--stroke-3-min) var(--stroke-1) 0px #000,
               var(--stroke-3) var(--stroke-1-min) 0px #000,
               var(--stroke-2) var(--stroke-2-min) 0px #000,
               var(--stroke-1) var(--stroke-3-min) 0px #000,
               0px var(--stroke-3-min) 0px #000,
               var(--stroke-1-min) var(--stroke-3-min) 0px #000,
               var(--stroke-2-min) var(--stroke-2-min) 0px #000,
               var(--stroke-3-min) var(--stroke-1-min) 0px #000;
 
}`;

const sms = `
/* Animation for SMS messages */
@keyframes messageEntrance {
  0% {
    transform: translate(-50px, 50px) scale(0.5);
    opacity: 0;
  }
  60% {
    transform: translate(0, 0) scale(1.01); /* Overshoot to be slightly too big */
    opacity: 1;
  }
  100% {
    transform: translate(0, 0) scale(1); /* Return to normal size */
    opacity: 1;
  }
}

/* Animation for message exit */
@keyframes messageExit {
  0% {
    transform: translate(0, 0) scale(1);
    opacity: 1;
  }
  40% {
    transform: translate(0, 0) scale(1.03); /* Slight bounce outward */
    opacity: 1;
  }
  100% {
    transform: translate(-500px, 0px) scale(0.5); /* Mirror of entrance animation */
    opacity: 0;
  }
}

/* SMS Theme CSS */
.chat_line {
  display: flex;
  flex-direction: column;
  position: relative;
  margin-bottom: 0.3em;
  padding-top: 1.9em !important; /* Space for username */
  width: 100%;
  color: black;
  animation: messageEntrance 0.5s cubic-bezier(0.175, 0.885, 0.32, 1.275); /* Bouncy easing function */
  transform-origin: bottom left; /* Set origin for the zoom */
  overflow: visible; /* Ensure animations don't get clipped */
}

/* Prevent animation for older messages when new ones are added */
.chat_line:not(:last-child) {
  animation: none;
}

/* Animation for messages being removed */
.chat_line.fading-out {
  animation: messageExit 0.7s cubic-bezier(0.175, 0.885, 0.32, 1.275) forwards; /* Bouncy easing function */
  overflow: visible;
}

/* Container adjustments */
#chat_container {
  overflow: visible; /* Ensure animations aren't clipped */
  padding: 10px; /* Add padding to the container */
}

/* Username pill styling */
.user_info {
  position: absolute;
  top: 0.3em;
  z-index: 2;
  padding: 0.2em 0.8em;
  border-radius: 1.2em;
  border: 0.1em solid #000;
  transform: rotate(-5deg);
  box-shadow: 2px 2px 5px rgba(0, 0, 0, 0.2);
  max-width: 80%;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  background-color: #777; /* Default color, will be overridden by JS */
}

.colon {
  display: none !important;
}

/* Override user color to apply to background instead */
.user_info .nick {
  color: white !important; /* Force white text for contrast */
  font-weight: bold;
  padding: 0;
  max-width: 100%;
}

.mention {
  color: black !important;
}

/* Message bubble styling */
.message {
  position: relative;
  width: fit-content; /* Better for dynamic content sizing */
  max-width: 10em;
  display: inline-block;
  padding: 0.7em 1em;
  margin-left: 1em;
  margin-right: 1em;
  border: 0.1em solid #000;
  border-radius: 0.8em;
  word-wrap: break-word;
  background-color: #f0f0f0; /* Default color, will be overridden by JS */
  --arrow-color: #f0f0f0; /* New CSS variable for the arrow color */
}

/* Speech bubble arrow */
.message:before {
  content: "";
  position: absolute;
  left: -1em;
  top: 1em;
  width: 0;
  height: 0;
  border-top: 0 solid transparent;
  border-bottom: 0.7em solid transparent;
  border-right: 1em solid #000; /* Black outline */
  z-index: 1;
}

.message:after {
  content: "";
  position: absolute;
  left: -0.8em; /* Adjusted for perfect alignment */
  top: 1.06em; /* Slight adjustment for perfect alignment */
  width: 0;
  height: 0;
  border-top: 0 solid transparent;
  border-bottom: 0.6em solid transparent; /* Adjusted to match border width */
  border-right: 0.85em solid var(--arrow-color); /* Adjusted to match border width */
  z-index: 2;
}

/* Custom image in corner */
.message-image {
  position: absolute;
  bottom: -0.3em;
  right: -1.1em;
  width: 2em;
  height: 2em;
  object-fit: contain;
  z-index: 3;
}`;
