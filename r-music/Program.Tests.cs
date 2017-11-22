using Xunit;

namespace r_listentothis
{
    
    public class ProgramTests
    {
        private Options _options;
        public ProgramTests()
        {
            _options = new Options()
            {
                folder = "C:\\douglasea\\music"
            };
        }
        
        [Fact]
        public void Program_GenerateSavePath_ShouldCombineNameFolderAndExtension()
        {
            Assert.Equal("C:\\douglasea\\music\\song.mp3", Program.GenerateSavePath("song", _options.folder, "mp3"));        
        }
        
        
    }
}