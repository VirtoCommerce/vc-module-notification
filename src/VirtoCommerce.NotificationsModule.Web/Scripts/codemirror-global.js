// The platform already ships CodeMirror in its vendor bundle, but without the fold addons.
// Webpack replaces the addons' relative `require('../../lib/codemirror')` with this shim
// (see NormalModuleReplacementPlugin in webpack.config.js) so they patch the platform's
// single global instance instead of bundling — and patching — a second copy.
// Not an AngularJS file: it only re-exports the global, so being picked up as a webpack
// entry by the Scripts/**/*.js glob is harmless.
module.exports = window.CodeMirror;
