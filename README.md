# Dash

Dash is a high performance dynamic reporting engine. It allows users to configure database connections, import database schema, and easily generate reports and charts without having any SQL knowledge.  It's a labor of love - created to experiment. There are options like ELK that do more; but Dash may work for you if you want something simple. 

* AspNetCore MVC 2.1 backend using Dapper for DB connections.
* Uses pJax to simulate a SPA experience without the overhead.
* Designed to use a SQL Server db, and query SQL Server, MySql/MariaDB, and Postgres (coming soon) databases.
* Every frontend component in the project has been chosen for performance, with many created custom or extensively modified for this project. Including but not limited to:
	* [Alertify](https://github.com/alertifyjs/alertify.js)
	* [Autocomplete](https://github.com/Pixabay/JavaScript-autoComplete)
	* [ChartJS](https://github.com/chartjs/Chart.js)
	* [CollapsibleList](http://code.stephenmorley.org/)
	* [doT](https://github.com/olado/doT)
	* [Draggabilly](http://draggabilly.desandro.com)
	* [FlexiColorPicker](https://github.com/DavidDurman/FlexiColorPicker)
	* [flatpickr](https://github.com/flatpickr/flatpickr)
	* [Fontello](http://fontello.com/)
	* [pJax](https://github.com/thybag/PJAX-Standalone)
	* [Spectre styles](https://github.com/picturepan2/spectre)
	* ... and a lot of native javascript.
* Designed to work on desktop and mobile devices.
* Supports Chrome 42+(preferred), Firefox 39+, Edge 14+, Opera 29+, Android 5+, Safari 10.3+.
	* Usage will be challenging/limited on devices smaller than 1024x768.
* Users can create custom reports from any SQL data source.
  * Use those reports to create charts and alerts.
  * Reports can be exported to Excel, and charts can be exported to PNG.
  * Create your own personalized dashboard displaying the reports and charts you choose. 
  * Dashboard widgets can be resized and arranged easily, and can refresh automatically.
* Multilingual support - limited to English and Spanish for now, but could be easily extended to include other languages.
* Context sensitive help teaches you how it works.

## [The Cranky Developer's Manifesto](https://dev.to/codemouse92/the-cranky-developer-manifesto--24km)

I am developing this project for the sole purpose of my own enjoyment. I make no promises about release date, features, usability, stability, practicality, or compliance with any normal standards of software development.

In pursuit of my unhindered enjoyment of this project, the only end-user I choose to care about in this project is myself, and maybe a few select friends. The timeline, the features, and the implementation are all solely at my discretion. I reserve the right to make arbitrary decisions, and change them at a moment's notice, without owing anyone an explanation.

If you see something here you like, you're welcome to fork the code under the terms of the LICENSE and do as you wish with it.

If you're still intent on treating this as a viable project, you're welcome to submit issues and pull requests. I may respond, or I may leave it sitting indefinitely. If I ignore your bug report or brilliant contribution until doomsday, don't take it personally.

If you decide to try and *use* this software, you're taking your sanity into your own hands. As long as it runs on my machine, that is all I care about. It may be unstable, or not support your system. I offer neither warranty nor technical support.

Long story short, I'm just coding for the love of coding!
