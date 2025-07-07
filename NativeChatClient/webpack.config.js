module.exports = (env, argv) => {
  const mode = argv.mode || 'development';
  return require(`./webpack/webpack.${mode}.js`);
};