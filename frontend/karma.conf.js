// Minimal Karma overrides for CI. The Angular builder (@angular/build:karma) supplies the
// frameworks (jasmine), plugins and file list; here we only add a headless Chrome launcher
// with --no-sandbox so tests run on CI runners (which forbid the Chrome sandbox).
module.exports = function (config) {
  config.set({
    frameworks: ['jasmine'],
    browsers: ['ChromeHeadlessNoSandbox'],
    customLaunchers: {
      ChromeHeadlessNoSandbox: {
        base: 'ChromeHeadless',
        flags: ['--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage'],
      },
    },
  });
};
