function encodeQueryData(data) {
  // https://stackoverflow.com/questions/111529/how-to-create-query-parameters-in-javascript
  const ret = [];
  for (let d in data) {
    if (data[d])
      ret.push(encodeURIComponent(d) + "=" + encodeURIComponent(data[d]));
  }
  return ret.join("&");
}

function appendCSS(type, name) {
  $("<link/>", {
    rel: "stylesheet",
    type: "text/css",
    class: `preview_${type}_${name}`,
    href: addRandomQueryString(`styles/${type}_${name}.css`),
  }).appendTo("head");
}

function removeCSS(type, name) {
  if (name) {
    $(`link[class="preview_${type}_${name}"]`).remove();
  } else {
    $(`link[class^="preview_${type}"]`).remove();
  }
}

function applyStyles(styleId, cssContent) {
  // Construct the element ID based on the style category
  const elementId = `dynamic-styles-${styleId}`;
  let styleElement = $(`#${elementId}`);

  // Check if the style element already exists
  if (styleElement.length === 0) {
    // If not, create a new one with the specific ID
    styleElement = $(`<style id="${elementId}" type="text/css"></style>`);
    $("head").append(styleElement);
  }

  // Set the CSS rules as the content of the style element
  styleElement.html(cssContent);
}

function removeStyles(styleId) {
  $(`#dynamic-styles-${styleId}`).remove();
}

function addRandomQueryString(url) {
  return url + (url.indexOf("?") >= 0 ? "&" : "?") + "v=" + Date.now();
}

function removeRandomQueryString(url) {
  return url.replace(/[?&]v=[^&]+/, "");
}
