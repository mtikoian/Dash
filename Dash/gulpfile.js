/// <binding ProjectOpened='watch' />
/* eslint-disable */

var addsrc = require('gulp-add-src'),
    autoprefixer = require('autoprefixer'),
    cleancss = require('gulp-clean-css'),
    concat = require('gulp-concat'),
    del = require('del'),
    gulp = require('gulp'),
    plumber = require('gulp-plumber'),
    postcss = require('gulp-postcss'),
    replace = require('gulp-replace-path'),
    sass = require('gulp-sass'),
    sourcemaps = require('gulp-sourcemaps'),
    through = require('through2'),
    uglify = require('gulp-uglify'),
    xml2js = require('xml2js');

var paths = {
    js: './Scripts/',
    css: './Content/',
    dist: './wwwroot/'
};

var includeSort = function(a, b) {
    return a.$.Include > b.$.Include ? 1 : a.$.Include < b.$.Include ? -1 : 0;
}

function sortXml(file, encoding, callback) {
    var parser = new xml2js.Parser({
        trim: true,
        preserveChildrenOrder: true
    });
    parser.parseString(file.contents.toString(), function(err, result) {
        var builder = new xml2js.Builder({
            renderOpts: {
                pretty: true,
                indent: '  ',
                newline: '\n'
            },
            xmldec: {
                encoding: 'utf-8'
            },
            allowSurrogateChars: true,
            cdata: true
        });
        result.Project.ItemGroup = result.Project.ItemGroup.map(function(x) {
            var newObject = {};
            var keys = Object.keys(x);
            keys.sort();
            keys.forEach(function(y) {
                newObject[y] = x[y];
                newObject[y].sort(includeSort);
            });
            return newObject;
        });
        file.contents = new Buffer(String(builder.buildObject(result)));
    });
    callback(null, file)
}

gulp.task('watch', function() {
    gulp.watch(paths.css + '**/*.scss', ['sass']);
    gulp.watch(paths.css + 'fontello/font/dash.*', ['fonts']);
    gulp.watch(paths.js + '**/*.js', ['js']);
    gulp.watch('*.csproj', ['webFixer']);
    gulp.watch('../Dash.Database/*.sqlproj', ['dbFixer']);
});

gulp.task('sass', function() {
    return gulp.src([paths.css + 'spectre/spectre.scss', paths.css + '*.scss'])
        .pipe(plumber())
        .pipe(sourcemaps.init())
        .pipe(sass().on('error', sass.logError))
        .pipe(postcss([autoprefixer()]))
        .pipe(addsrc.append(paths.css + 'fontello/css/dash.css'))
        .pipe(concat('bundle.css'))
        .pipe(cleancss())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(paths.dist + 'css/'));
});

gulp.task('js', function() {
    return gulp.src([
        // core libraries and helpers
        paths.js + 'polyfills.js',         // Polyfills for promise/fetch
        paths.js + 'mithril.js',           // mithril rendering library, includes promise polyfill for ie
        paths.js + 'core.js',              // common js functions for the site
        paths.js + 'Alertify.js',          // alerts/modals
        paths.js + 'pjax.js',              // pjax library
        paths.js + 'helpers.js',           // commonly used helper libraries
        // re-used components
        paths.js + 'Autocomplete.js',      // autocomplete component
        paths.js + 'Chart.js',             // charting
        paths.js + 'CollapsibleList.js',   // lightweight library for treeviews
        paths.js + 'ColorPicker.js',       // custom color picker
        paths.js + 'DashChart.js',         // chart wrapper
        paths.js + 'DatePicker.js',        // custom date component using mithril
        paths.js + 'Draggabilly.js',       // drag-n-drop functionality
        paths.js + 'Help.js',              // custom help component using mithril
        paths.js + 'Rect.js',              // library for rectangle geometry
        paths.js + 'Tab.js',               // Tab component
        paths.js + 'Table.js',             // custom table component using mithril
        paths.js + 'Widget.js',            // widget component using mithril
        // functional areas
        paths.js + 'Form.js',              // custom form component using mithril
        paths.js + 'shared.js',            // functionality shared for reports and charts
        paths.js + 'content.js',           // content processing functionality
        paths.js + 'reports.js',           // report functionality
        paths.js + 'charts.js',            // chart functionality
        paths.js + 'dashboard.js'          // dashboard functionality
    ])
        .pipe(plumber())
        .pipe(sourcemaps.init())
        .pipe(concat('bundle.js'))
        .pipe(uglify())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(paths.dist + 'js/'));
});

// Fonts
gulp.task('fonts', function() {
    return gulp.src([
        paths.css + 'fontello/font/dash.*'
    ])
        .pipe(gulp.dest(paths.dist + 'font/'));
});

gulp.task('build', ['clean', 'js', 'sass', 'fonts', 'favicon']);

gulp.task('clean', function() {
    return del.sync(paths.dist + 'css', paths.dist + 'js', paths.dist + 'font');
});

gulp.task('favicon', function() {
    return gulp.src(['*.ico']).pipe(gulp.dest(paths.dist));
});

gulp.task('webFixer', function() {
    return gulp.src(['*.csproj']).pipe(through.obj(sortXml)).pipe(replace('&#xD;', '')).pipe(gulp.dest('.'));
});

gulp.task('dbFixer', function() {
    return gulp.src(['../Dash.Database/*.sqlproj']).pipe(through.obj(sortXml)).pipe(replace('&#xD;', '')).pipe(gulp.dest('../Dash.Database/'));
});
