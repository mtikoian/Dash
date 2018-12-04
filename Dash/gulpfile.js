/// <binding ProjectOpened='watch' />

var addsrc = require('gulp-add-src'),
    autoprefixer = require('autoprefixer'),
    cleancss = require('gulp-clean-css'),
    concat = require('gulp-concat'),
    del = require('del'),
    gulp = require('gulp'),
    gzip = require('gulp-gzip'),
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

var childSort = function(a, b) {
    if (a.$.Include) {
        return a.$.Include > b.$.Include ? 1 : a.$.Include < b.$.Include ? -1 : 0;
    }
    return a.$.Update > b.$.Update ? 1 : a.$.Update < b.$.Update ? -1 : 0;
};

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
                newObject[y].sort(childSort);
            });
            return newObject;
        });
        file.contents = new Buffer(String(builder.buildObject(result)));
    });
    callback(null, file);
}

function sassFiles() {
    return gulp.src([paths.css + 'spectre/spectre.scss', paths.css + '*.scss'])
        .pipe(plumber())
        .pipe(sourcemaps.init())
        .pipe(sass().on('error', sass.logError))
        .pipe(postcss([autoprefixer()]))
        .pipe(addsrc.append(paths.css + 'fontello/css/dash.css'))
        .pipe(concat('bundle.css'))
        .pipe(cleancss())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(paths.dist + 'css/'))
        .pipe(gzip({ append: true }))
        .pipe(gulp.dest(paths.dist + 'css/'));
}

function jsFiles() {
    return gulp.src([
        // core libraries and helpers
        paths.js + 'polyfills.js',         // Polyfills for promise/fetch
        paths.js + 'core.js',              // common js functions for the site
        paths.js + 'Alertify.js',          // alerts/modals
        paths.js + 'pjax.js',              // pjax library
        paths.js + 'ajax.js',              // ajax helper library
        paths.js + 'accounting.js',        // currency helper library
        paths.js + 'doT.js',               // template engine
        paths.js + 'flatpickr.js',         // datetime picker
        // re-used components
        paths.js + 'Autocomplete.js',      // autocomplete component
        paths.js + 'Chart.js',             // charting
        paths.js + 'CollapsibleList.js',   // lightweight library for treeviews
        paths.js + 'DashChart.js',         // chart wrapper
        paths.js + 'Draggabilly.js',       // drag-n-drop functionality
        paths.js + 'ColorPicker.js',       // custom color picker
        paths.js + 'Rect.js',              // library for rectangle geometry
        paths.js + 'doTable.js',           // new table component
        paths.js + 'Widget.js',            // widget component using mithril
        // functional areas
        paths.js + 'content.js'           // content processing functionality
    ])
        .pipe(plumber())
        .pipe(sourcemaps.init())
        .pipe(concat('bundle.js'))
        .pipe(uglify())
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(paths.dist + 'js/'))
        .pipe(gzip({ append: true }))
        .pipe(gulp.dest(paths.dist + 'js/'));
}

// Fonts
function fonts() {
    return gulp.src([
        paths.css + 'fontello/font/dash.*'
    ])
        .pipe(gulp.dest(paths.dist + 'font/'))
        .pipe(gzip({ append: true }))
        .pipe(gulp.dest(paths.dist + 'font/'));
}

function clean(done) {
    del.sync(paths.dist + 'css', paths.dist + 'js', paths.dist + 'font');
    done();
}

function favicon() {
    return gulp.src(['*.ico']).pipe(gulp.dest(paths.dist));
}

function webFixer() {
    return gulp.src(['*.csproj']).pipe(through.obj(sortXml)).pipe(replace('&#xD;', '')).pipe(gulp.dest('.'));
}

function dbFixer() {
    return gulp.src(['../Dash.Database/*.sqlproj']).pipe(through.obj(sortXml)).pipe(replace('&#xD;', '')).pipe(gulp.dest('../Dash.Database/'));
}

function watchFiles() {
    gulp.watch(paths.css + '**/*.scss', sassFiles);
    gulp.watch(paths.css + 'fontello/font/dash.*', fonts);
    gulp.watch(paths.js + '**/*.js', jsFiles);
    gulp.watch('*.csproj', webFixer);
    gulp.watch('../Dash.Database/*.sqlproj', dbFixer);
}

gulp.task('sass', sassFiles);
gulp.task('js', jsFiles);
gulp.task('fonts', fonts);
gulp.task('clean', clean);
gulp.task('favicon', favicon);
gulp.task('webFixer', webFixer);
gulp.task('dbFixer', dbFixer);
gulp.task('build', gulp.series('clean', gulp.parallel(jsFiles, sassFiles, fonts, favicon)));
gulp.task('watch', watchFiles);
