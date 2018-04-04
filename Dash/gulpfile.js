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
    gulp.watch(paths.js + '**/*.js', ['min:js:core', 'min:js:modules']);
    gulp.watch('*.csproj', ['webFixer']);
    gulp.watch('../Dash.Database/*.sqlproj', ['dbFixer']);
    gulp.watch('../Dash.I18n/*.csproj', ['i18nFixer']);
});

gulp.task('sass', function() {
    //return gulp.src([paths.css + 'bootstrap/bootstrap.scss', paths.css + '*.scss'])
    return gulp.src([paths.css + 'spectre/spectre.scss', paths.css + '*.scss'])
        .pipe(plumber())
        .pipe(sourcemaps.init())
        .pipe(sass().on('error', sass.logError))
        .pipe(postcss([autoprefixer()]))
        .pipe(addsrc.append(paths.css + 'fontello/css/dash.css'))
        .pipe(concat('core.css'))
        .pipe(cleancss())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(paths.dist + 'css/'));
});

gulp.task('min:js:core', function() {
    return gulp.src([
        paths.js + 'CustomEvent.js',       // customEvent polyfill for ie
        paths.js + 'mithril.js',           // mithril rendering library, includes promise polyfill for ie
        paths.js + 'core.js',              // common js functions for the site
        paths.js + 'Alertify.js',          // alerts/modals
        paths.js + 'ajax.js',              // ajax request handling
        paths.js + 'events.js',            // custom events
        paths.js + 'resx.js'               // resource handling
    ])
        .pipe(plumber())
        .pipe(sourcemaps.init())
        .pipe(concat('core.js'))
        .pipe(uglify())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(paths.dist + 'js/'));
});

gulp.task('min:js:modules', function() {
    return gulp.src([
        paths.js + 'native.bootstrap.js',  // custom build of the native bootstrap project with just the functionality needed
        paths.js + 'fecha.js',             // lightweight alternative to moment for date manipulation
        paths.js + 'accounting.js',        // number/currency formatting
        paths.js + 'Help.js',              // custom help component using mithril
        paths.js + 'Dialog.js',            // custom dialog component using mithril
        paths.js + 'Autocomplete.js',      // custom autocomplete component using mithril
        paths.js + 'DatePicker.js',        // custom date component using mithril
        paths.js + 'Rect.js',              // library for rectangle geometry
        paths.js + 'Draggabilly.js',       // drag-n-drop functionality
        paths.js + 'Chart.js',             // charting
        paths.js + 'Prism.js',             // syntax highlighting for sql
        paths.js + 'CollapsibleList.js',   // lightweight library for treeviews
        paths.js + 'Validator.js',         // form validation using html5 and bootstrap
        paths.js + 'colors.js',            // color library
        paths.js + 'ColorPicker.js',       // custom color picker
        paths.js + 'Table.js',             // custom table component using mithril
        paths.js + 'DashChart.js',         // chart wrapper
        paths.js + 'Form.js',              // custom form component using mithril
        paths.js + 'JoinForm.js',          // dataset joins form functionality
        paths.js + 'ColumnForm.js',        // dataset columns form functionality
        paths.js + 'ShareForm.js',         // share report form functionality
        paths.js + 'FilterForm.js',        // report filter form functionality
        paths.js + 'GroupForm.js',         // report filter form functionality
        paths.js + 'RangeForm.js',         // chart range form functionality
        paths.js + 'Dataset.js',           // dataset functionality
        paths.js + 'BaseDetails.js',       // report/chart details shared functionality
        paths.js + 'ReportDetails.js',     // report functionality
        paths.js + 'ChartDetails.js',      // chart functionality
        paths.js + 'Widget.js',            // custom classes
        paths.js + 'dialogs.js',           // custom alerts/modals functionality
        paths.js + 'datasets.js',          // dataset interfaces
        paths.js + 'reports.js',           // report interfaces
        paths.js + 'charts.js',            // chart interfaces
        paths.js + 'dashboard.js'          // dashboard functionality
    ])
        .pipe(plumber())
        .pipe(sourcemaps.init())
        .pipe(concat('modules.js'))
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

gulp.task('build', ['clean', 'min:js:core', 'min:js:modules', 'sass', 'fonts', 'favicon']);

gulp.task('clean', function() {
    return del.sync(paths.dist + 'css', paths.dist + 'js', paths.dist + 'font');
});

gulp.task('favicon', function() {
    return gulp.src(['*.ico']).pipe(gulp.dest(paths.dist));
});

gulp.task('webFixer', function() {
    return gulp.src(['*.csproj']).pipe(through.obj(sortXml)).pipe(replace('&#xD;', '')).pipe(gulp.dest('.'));
});

gulp.task('i18nFixer', function() {
    return gulp.src(['../Dash.I18n/*.csproj']).pipe(through.obj(sortXml)).pipe(replace('&#xD;', '')).pipe(gulp.dest('../Dash.I18n/'));
});

gulp.task('dbFixer', function() {
    return gulp.src(['../Dash.Database/*.sqlproj']).pipe(through.obj(sortXml)).pipe(replace('&#xD;', '')).pipe(gulp.dest('../Dash.Database/'));
});
