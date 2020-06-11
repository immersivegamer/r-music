using System.Collections.Generic;
using Moq;
using RedditSharp.Things;
using Xunit;

namespace r_listentothis
{
    
    public class ProgramTests
    {
        private Options _options;
        private List<Post> _posts;
        private Program _program;
        private Mock<Program> _mockProgram;
        public ProgramTests()
        {
            _posts = new List<Post>();
            _program = new Program();
            _mockProgram = new Mock<Program>();
            _options = new Options()
            {
                folder = "C:\\douglasea\\music",
                commentThreshold = 0,
                count = 5,
                upvoteThreshold = 0,
                sort = Options.ListingType.All
            };
        }
        
        [Fact]
        public void Program_GenerateSavePath_ShouldCombineNameFolderAndExtension()
        {
            Assert.Equal("C:\\douglasea\\music\\song.mp3", _program.GenerateSavePath("song", _options.folder, "mp3"));        
        }

        [Fact]
        public void Program_GenerateSavePath_ShouldCleanFileNamesWithUnderscores()
        {
            Assert.Equal("C:\\douglasea\\music\\s_o_n_g.mp3", _program.GenerateSavePath("s<o>:n\"/g|?*", _options.folder, "mp3"));
        }

        [Fact]
        public void Program_RemoveDuplicatePosts_ShouldReturnZeroWhenNoPosts()
        {
            Assert.Equal(0, _program.RemoveDuplicatePosts(_posts, _options));
        }
        
        [Fact]
        public void Program_RemoveDuplicatePosts_ShouldReturnZeroWhenNoDuplicates()
        {
            _posts.Add(new Post(){Title="title"});
            _posts.Add(new Post(){Title="title2"});
            _mockProgram.Setup(p => p.Exists(It.IsAny<string>())).Returns(false);
            Assert.Equal(0, _program.RemoveDuplicatePosts(_posts, _options));
        }
        
        [Fact]
        public void Program_RemoveDuplicatePosts_ShouldReturnTheNumberOfDuplicates()
        {
            _posts.Add(new Post(){Title="title"});
            _posts.Add(new Post(){Title="title2"});
            _mockProgram.Setup(p => p.Exists(It.IsAny<string>())).Returns(true);
            Assert.Equal(2, _mockProgram.Object.RemoveDuplicatePosts(_posts, _options));
        }
    }
}