export default function Overview({user}){

return(

<>

<section className="profile-card">

<h2>Account Details</h2>

<p><b>Username:</b> {user?.userName}</p>

<p><b>Email:</b> {user?.email}</p>

<p><b>Joined:</b> {user?.createdAt}</p>

</section>

<section className="profile-card">

<h2>Quick Actions</h2>

<button>Edit Profile</button>

<button>Change Password</button>

</section>

</>

);

}