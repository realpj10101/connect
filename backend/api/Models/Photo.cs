namespace api.Models;

public record Photo(
    string Url_165, // navbar 165 * 165 10kB
    string Url_256, // card / thumbnail 17kB
    string Url_enlarged // enlarged photo up to ~300kb
);