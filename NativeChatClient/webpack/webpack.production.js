const { merge } = require("webpack-merge");
const common = require("./webpack.common");

module.exports = merge(common, {
  mode: "production",
  devtool: false, // or 'source-map' if you want to ship source maps
  optimization: {
    minimize: true,
  },
});