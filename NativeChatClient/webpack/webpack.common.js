const path = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const { CleanWebpackPlugin } = require("clean-webpack-plugin");
const CopyWebpackPlugin = require("copy-webpack-plugin");

module.exports = {
  entry: {
    index: "./src/index.html",
    v2: "./src/v2/index.html",
  },
  output: {
    filename: "[name].[contenthash].js",
    path: path.resolve(__dirname, "../dist"),
    clean: true,
  },
  module: {
    rules: [
      {
        test: /\.html$/,
        use: ["html-loader"],
      },
      {
        test: /\.(png|svg|jpg|gif|webp)$/,
        type: "asset/resource",
      },
      {
        test: /\.(woff|woff2|eot|ttf|otf)$/,
        type: "asset/resource",
      },
    ],
  },
  plugins: [
    new CleanWebpackPlugin(),
    new HtmlWebpackPlugin({
      template: "./src/index.html",
      filename: "index.html",
      inject: true,
      hash: true,
    }),
    new HtmlWebpackPlugin({
      template: "./src/v2/index.html",
      filename: "v2/index.html",
      inject: true,
      hash: true,
    }),
    new CopyWebpackPlugin({
      patterns: [
        { from: "src/styles", to: "styles" },
        { from: "src/v2/styles", to: "v2/styles" },
        { from: "src/fonts", to: "" },
        { from: "src/media", to: "media" },
        { from: "src/embed", to: "" },
      ],
    }),
  ],
};