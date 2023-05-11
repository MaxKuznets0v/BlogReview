# BlogReview - web site for posting reviews
### Site link: https://maxkuz-001-site1.etempurl.com

### Technology stack
- .Net 6 
- ASP.NET Core MVC
- EF Core
- MySQL
- SignalR
- JavaScript, Bootstrap

### Description
- Site contains 7 pages: main, review page, login page, user profile, create review, search page, admin panel.
- Non-authenticated users have read-only access (they can use search, but can’t create reviews, can’t leave comments, rating and likes).
- Authenticated users can have 3 roles: User, Admin, MasterAdmin. User has the same rights as non-authenticated users but also can create and edit their own content (reviews, comments, likes, grades). Admin has the same rights as User but also edit content from other Users (create reviews, edit them, remove comments). But Admins can't do this with MasterAdmin content. Also, Admins have access to Admin panel where he can block and delete other Users (but can't do anything with Admins and MasterAdmins). Master Admin can do anything with other Users contents (including Admin). He can also give and remove admin rights from users (except another MasterAdmin).
- There are two ways for authentication: Google+ and LinkedIn. 
- Every page has fulltext search over whole app using MySQL Fulltext indexes.
- Every user has its own profile with a list of reviews.
- Reviews on the same piece of art can be connected. 
- Each review can have arbitrary number of images.
- Reviews can be exported with images as a pdf.
- Site has two language options: english and russian.
- Site has two theme options: light and dark.
- Site layout has adaptive styles for small screens.

#### Navigation bar
At the top of the every page is a navigation bar with 
- `BlogReview` name, which is a link to main page.
-  Link to main page.
-  Link to admin panel if user has admin rights.
-  Search field.
-  Link to user profile if user was logged in.
-  Login button if user wasn't logged in.
-  Logout button if user was logged in. 
-  Toggle button for changing the theme (light/dark).
-  Option button for changing the language (English, Russian).

#### Main page
**Href: /, /Feed**

Main page shows recent reviews in chronological order. Review card shows a brief description with title, author and his rating, tag list, piece of art name 
and short review passage. If review has images the first one will be shown in the card. In order to read review user have to click `Read` button.

On the right side of the page there are top-5 reviews with highest grades. They have a title and a brief description with a link to the full text. 

It is a tag cloud below them in the same column. Cloud was made using `amcharts` javascript framework. Each tag have the frequency of usage and could be clicked.
When pushing the tag name a search page is opened with a list of reviews those have the same tag.

#### Review page
**Href: /Feed/Article/{id}**

This page contains full information about the review: 
- Title 
- Author (with rating and a profile link)
- Tags
- Publish date
- Author grade
- Piece of art (with rating)
- Category 
- Review content

Registered users can rate reviewing piece of art with grade from 1 to 5. These grades are used to calculate average piece of art rating.
Review author gives a grade from 1 to 10. This grade is shown at the bottom of review content. Registered users can give a like to a review. These likes
are used to calculated the author rating (rating = sum of likes). One person can give one like and piece of art grade per review. Likes are removable,
user grades are changable. 

If review has images they are shown using `slick` javascript framework in a circular manner. 

Below review content user can click `show similar reviews` and they will be moved to search page with a list of all reviews for given piece of art. 

Next to this users can click `Export to PDF` button. This button saves review description, its content and images to a pdf and downloads it.

At the bottom of the page there is a comment section. Registered users can leave their comments. They are shown in chronlogical linear order. Comment section was implemented using `signalR` framework. This way any changes (creating or removal of the comment) presented in a real-time. Each comment has its text, author name and a link to the profile. Comments can be removed by an author of a comment or a user with admin rights.

If user allowed to edit the article (is its author or has admin rights) `edit` button is also shown.

#### Create review page
**Href: /Feed/CreateArticle**

This page has a form for with following windows:
- Title (required)
- Content (text window wit markdown editor, implemented with `SimpleMDE` javascript framework for markdown format. required)
- Rating (whole number from 1 to 10, required)
- Piece of art (either can be picked from existing list or typed in manually. Provides hints with existing entries. required)
- Piece of art group (piece of art category. Can be picked only if new piece of art typed. Otherwise, has Piece of art group value)
- Tags (a list with tag for review. Can have multiple entries, shows hints while typing. optional)
- Images (a lsit with review images. implemented using `dropzone` javascript framework. optional)

Review images are stored in `cloudinary` cloud on the server-side.

#### User profile
**Href: /Account?userId={id}**

This page contains a list of user's reviews in a table. Table is implemented using `datatables` javascript framework. Each column (title, reviewing piece, rating) is sortable. There is also a filter field for searching for a specific entry. If review is editable for current user (author or user with admin rights) there are `edit`, `delete` and `Create new article` buttons. `View` button is always visible. If user profile is editable - then current user can change profile nickname with `Change username` button. Nicknames are changed everywhere across the app. 

#### Login page
**Href: /Account/Login**

The page provides login feature: with Google and LinkedIn profiles. If user was blocked the message will occur. If user was deleted user will be able to login but all of his content will be lost. After successfull login user name will be shown in the navigation bar.

#### Search page
**Href: /Feed/Search?query={query}**

The page shows the results of the search: list of appropriate reviews (which contain user's query). The structure of cards are the same as on main page.

#### Admin panel
**Href: /Account/Admin**

This page helps to manage user profiles. It requires admin rights to enter. it contains userId, username, user role and email. Admin can block and delete users and view their profiles. Admin can't block, delete or set/remove admin rights of other Admins or MasterAdmins. MasterAdmin can block and delete users and view their profiles and also block, delete or set/remove admin rights from Admin users. But can't do anything except viewing other MasterAdmin profiles. All MasterAdmins are set while app is loading at the first time.

## Database schema
![BlogReviewDB](https://github.com/MaxKuznets0v/BlogReview/assets/44207354/8e021c38-a07f-49ef-a58e-c7f536df571e)

